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

        static int[] allowedSenderHeight = { 1081, 1082, 1083 };

        // Separate dictionaries for plugins and shared textures
        static Dictionary<string, IntPtr> senderPlugins = new Dictionary<string, IntPtr>();
        static Dictionary<string, UnityEngine.Texture2D> sharedTextures = new Dictionary<string, UnityEngine.Texture2D>();
        static Dictionary<string, UnityEngine.RenderTexture> tmpTextures = new Dictionary<string, UnityEngine.RenderTexture>();

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
                GetOrCreateReceiverPlugin("OBS1", 721); 
                GetOrCreateReceiverPlugin("OBS2", 722);
                GetOrCreateReceiverPlugin("OBS3", 723);
                Engine.Current.WorldManager.WorldAdded += (World w) =>
                {
                    Msg("world focused");
                };
            });

        }

        static IntPtr GetOrCreateSenderPlugin(int width, int height)
        {
            string key = Util.getNameFromTextureResolution(width, height);

            if (!senderPlugins.ContainsKey(key))
            {
                Msg($"Creating new sender plugin for resolution: {key}");
                IntPtr plugin = PluginEntry.CreateSender(key, width, height);
                if (plugin == IntPtr.Zero)
                {
                    Msg("Failed to create Spout sender");
                    return IntPtr.Zero;
                }

                senderPlugins.Add(key, plugin);
                Msg($"Created new sender plugin for resolution: {key}");
            }

            return senderPlugins[key];
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

        static UnityEngine.Texture2D GetOrCreateSharedTexture(IntPtr plugin)
        {
            if (plugin == IntPtr.Zero)
            {
                return null;
            }

            // get key from plugins
            string key = senderPlugins.FirstOrDefault(x => x.Value == plugin).Key;

            if (!sharedTextures.ContainsKey(key))
            {
                var ptr = PluginEntry.GetTexturePointer(plugin);
                if (ptr != IntPtr.Zero)
                {
                    UnityEngine.Texture2D sharedTexture = UnityEngine.Texture2D.CreateExternalTexture(
                        PluginEntry.GetTextureWidth(plugin),
                        PluginEntry.GetTextureHeight(plugin),
                        UnityEngine.TextureFormat.ARGB32, false, false, ptr
                    );
                    sharedTexture.hideFlags = HideFlags.DontSave;
                    sharedTextures.Add(key, sharedTexture);
                    Msg(key + " texture created");
                }
            }

            return sharedTextures[key];
        }

        [HarmonyPatch]
        class CameraPatch
        {
            static void SendRenderTexture(UnityEngine.RenderTexture source)
            {
                IntPtr plugin = GetOrCreateSenderPlugin(source.width, source.height);
                SpoutUtil.IssuePluginEvent(PluginEntry.Event.Update, plugin);
                UnityEngine.Texture2D sharedTexture = GetOrCreateSharedTexture(plugin);

                if (plugin == IntPtr.Zero || sharedTexture == null)
                {
                    Msg("Spout not ready or sharedTexture is null");
                    return;
                }

                var tempRT = UnityEngine.RenderTexture.GetTemporary(sharedTexture.width, sharedTexture.height);
                Graphics.Blit(source, tempRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f));
                Graphics.CopyTexture(tempRT, sharedTexture);
                UnityEngine.RenderTexture.ReleaseTemporary(tempRT);
            }

            [HarmonyPatch(typeof(CameraRenderEx), "OnPreCull")]
            [HarmonyPostfix]
            static void _prefix(CameraRenderEx __instance)
            {
                var cam = __instance.Camera;

                if (!allowedSenderHeight.Contains(cam.targetTexture.height))
                {
                    return;
                }

                if (cam.enabled == false)
                {
                    return;
                }

                var _prevContext = RenderHelper.CurrentRenderingContext;
                RenderHelper.BeginRenderContext(RenderingContext.RenderToAsset);
                
                var tmpCameraRenderTexture = cam.targetTexture;

                UnityEngine.RenderTexture tempRenderTexture = null;
                if(tmpTextures.ContainsKey(Util.getNameFromTextureResolution(tmpCameraRenderTexture.width, tmpCameraRenderTexture.height)))
                {
                    // Msg(cam.targetTexture.width + "x" + cam.targetTexture.height + " already exists");
                    tempRenderTexture = tmpTextures[Util.getNameFromTextureResolution(tmpCameraRenderTexture.width, tmpCameraRenderTexture.height)];
                } else
                {
                    Msg("create new render texture " + Util.getNameFromTextureResolution(tmpCameraRenderTexture.width, tmpCameraRenderTexture.height));
                    tempRenderTexture = UnityEngine.RenderTexture.GetTemporary(tmpCameraRenderTexture.width, tmpCameraRenderTexture.height, 24);
                    tmpTextures.Add(Util.getNameFromTextureResolution(tempRenderTexture.width, tempRenderTexture.height), tempRenderTexture);
                }
                
                cam.targetTexture = tempRenderTexture;
                cam.nearClipPlane = 0.01f;
                cam.Render();

                cam.targetTexture = tmpCameraRenderTexture;

                RenderHelper.BeginRenderContext(_prevContext.Value);
            }


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
                            var tex = UnityEngine.Texture2D.CreateExternalTexture(width, height, UnityEngine.TextureFormat.R8, false, false, ptr);
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

                if (!allowedSenderHeight.Contains(src.height))
                {
                    return true;
                }

                var key = Util.getNameFromTextureResolution(src.width, src.height);
                if (tmpTextures.ContainsKey(key))
                {
                    var tex = tmpTextures[key];
                    SendRenderTexture(tex);
                }

                foreach (var p in senderPlugins)
                {
                    SpoutUtil.IssuePluginEvent(PluginEntry.Event.Update, p.Value);
                }
                return true;
            }
        }
    }
}
