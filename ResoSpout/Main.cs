using CppSharp.AST;
using Elements.Assets;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityFrooxEngineRunner;
using uOSC;
using static Leap.Unity.MultiTypedList;

namespace ResoSpout
{
    public class ResoSpout : ResoniteMod
    {
        public override string Name => "ResoSpout";
        public override string Author => "kka429";
        public override string Version => "0.0.1";
        public override string Link => "https://github.com/rassi0429/";

        public static bool isDone = false;

        public static int updateCount = 0;

        public static int SIZE = 512;
        public static int BATCH_SIZE = 32;


        static Dictionary<string, IntPtr> recieverPlugins = new Dictionary<string, IntPtr>();
        static Dictionary<string, UnityEngine.Texture2D> recieverTextures = new Dictionary<string, UnityEngine.Texture2D>();
        static Dictionary<string, int> recieverHeight = new Dictionary<string, int>();
        
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.kokoa.resospout");
            harmony.PatchAll();

            Engine.Current.RunPostInit(() =>
            {
                Msg("RunPostInit");
                GetOrCreateReceiverPlugin("point", 500); 
                Engine.Current.WorldManager.WorldAdded += (World w) =>
                {
                    Msg("world focused");
                };
            });

        }

        static IntPtr GetOrCreateReceiverPlugin(string key, int height)
        {
            if (!recieverPlugins.ContainsKey(key))
            {
                Msg($"Creating new receiver plugin for resolution: {key}");
                IntPtr plugin = PluginEntry.CreateReceiver(key);
                if (plugin == IntPtr.Zero)
                {
                    Msg("Failed to create Spout receiver");
                    return IntPtr.Zero;
                }

                recieverPlugins.Add(key, plugin);
                recieverHeight.Add(key, height);
                Msg($"Created new reveiver plugin for resolution: {key}");
            }
            return recieverPlugins[key];
        }

        static UnityEngine.Texture2D createReadabeTexture2D(UnityEngine.Texture2D texture2d)
        {
            UnityEngine.RenderTexture renderTexture = UnityEngine.RenderTexture.GetTemporary(
                        texture2d.width,
                        texture2d.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(texture2d, renderTexture);
            UnityEngine.RenderTexture previous = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = renderTexture;
            UnityEngine.Texture2D readableTextur2D = new UnityEngine.Texture2D(texture2d.width, texture2d.height);
            readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTextur2D.Apply();
            UnityEngine.RenderTexture.active = previous;
            UnityEngine.RenderTexture.ReleaseTemporary(renderTexture);
            return readableTextur2D;
        }


        [HarmonyPatch]
        class CameraPatch
        {
            [HarmonyPatch(typeof(PostProcessLayer), "OnRenderImage")]
            [HarmonyPrefix]
            static bool prefix(UnityEngine.RenderTexture src, UnityEngine.RenderTexture dst)
            {

                foreach (var _reciverPl in recieverPlugins)
                {
                    var _pl = _reciverPl.Value;
                    if (_pl != System.IntPtr.Zero)
                    {
                        SpoutUtil.IssuePluginEvent(PluginEntry.Event.Update, _pl);
                        IntPtr ptr = PluginEntry.GetTexturePointer(_pl);
                        var width = PluginEntry.GetTextureWidth(_pl);
                        var height = PluginEntry.GetTextureHeight(_pl);

                        //UnityEngine.Texture2D tex = null;

                        if (recieverTextures.ContainsKey(_reciverPl.Key))
                        {
                            recieverTextures[_reciverPl.Key].UpdateExternalTexture(ptr);
                        } else
                        {
                            var tex = UnityEngine.Texture2D.CreateExternalTexture(width, height, UnityEngine.TextureFormat.RGBA32, false, false, ptr);
                            tex.hideFlags = HideFlags.DontSave;
                            recieverTextures.Add(_reciverPl.Key, tex);
                        }

                        var _height = recieverHeight[_reciverPl.Key];
                        if(_height == dst.height)
                        {
                            Graphics.Blit(recieverTextures[_reciverPl.Key], dst, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f));
                            return false;
                        } 
                    }
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(FrooxEngine.Engine), "RunUpdateLoop")]
        class Patch
        {
            static bool Prefix(FrooxEngine.Engine __instance)
            {
                if (__instance.WorldManager.FocusedWorld == null) return true;

                __instance.WorldManager.FocusedWorld.RunSynchronously(() =>
                {

                    var pointMesh = __instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#PointMesh");
                    var pointMeshComponent = pointMesh.GetComponent<FrooxEngine.PointMesh>();

                    var t = __instance.WorldManager.FocusedWorld.Time.WorldTimeFloat;

                    updateCount++;

                    if (isDone)
                    {
                        var updateX = updateCount % (SIZE / BATCH_SIZE);
                        var x = updateX;
                        //if (updateCount % 100 != 0)
                        //{
                        //    return;
                        //}

                        // var colorArray2 = new Elements.Core.color[SIZE * SIZE];
                        //for (int x = 0; x < SIZE; x++)
                        //{
                        var tex = createReadabeTexture2D(recieverTextures["point"]);
                        // Msg(tex.graphicsFormat); // R8G8B8A8_SRGB
                        var colorChunkArray = new Elements.Core.color[SIZE * BATCH_SIZE];
                        for (int _x = 0; _x < BATCH_SIZE; _x++)
                        {
                            for (int y = 0; y < SIZE; y++)
                            {
                                
                                var c = tex.GetPixel(x * BATCH_SIZE + _x, y);
                                //Msg(c.r + " " + c.g + " " + c.b + " " + c.a);

                                colorChunkArray[_x * SIZE + y] = 
                                new Elements.Core.color(c.r, c.g ,c.b);
                            }
                        }
                        var _c = tex.GetPixel(200, 200);
                        
                        // Msg(_c.r + " " + _c.g + " " + _c.b);
                        pointMeshComponent.Colors.Write(colorChunkArray, x * SIZE * BATCH_SIZE);
                        //}
                        return;
                    };


                    Msg("PointMeshComponent : " + pointMeshComponent);
                    pointMeshComponent.Points.SetSize(SIZE * SIZE);
                    Msg("point setsize done");

                    var sizeArray = new Elements.Core.float2[SIZE * SIZE];
                    var colorArray = new Elements.Core.color[SIZE * SIZE];
                    var rotationArray = new float[SIZE * SIZE];
                    var pointArray = new Elements.Core.float3[SIZE * SIZE];

                    for (int x = 0; x < SIZE; x++)
                    {
                        Msg("x : " + x);
                        for (int y = 0; y < SIZE; y++)
                        {
                            sizeArray[x * SIZE + y] = new Elements.Core.float2(1f, 1f);
                            colorArray[x * SIZE + y] = new Elements.Core.color((float)x / SIZE, (float)y / SIZE, 0f, 1f);
                            rotationArray[x * SIZE + y] = 0f;
                            pointArray[x * SIZE + y] = new Elements.Core.float3(x / 100f, y / 100f, 0);
                            //pointMeshComponent.Sizes.Write(new Elements.Core.float2(1f, 1f), x * SIZE + y);
                            //pointMeshComponent.Colors.Write(new Elements.Core.color((float)x / SIZE, (float)y / SIZE, 0f, 1f), x * SIZE + y);
                            //pointMeshComponent.Rotations.Write(0f, x * SIZE + y);
                            //pointMeshComponent.Points.Write(new Elements.Core.float3(x / 100f, y/100f, 0), x * SIZE + y);
                        }
                    }

                    pointMeshComponent.Sizes.Write(sizeArray, 0);
                    pointMeshComponent.Colors.Write(colorArray, 0);
                    pointMeshComponent.Rotations.Write(rotationArray, 0);
                    pointMeshComponent.Points.Write(pointArray, 0);

                    isDone = true;

                });

                return true;
            }
        }

    }
}
