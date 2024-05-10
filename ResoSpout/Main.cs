using CppSharp.AST;
using Elements.Assets;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityFrooxEngineRunner;
using uOSC;
using static FrooxEngine.MeshEmitter;
using static UnityEngine.GraphicsBuffer;

namespace ResoSpout
{
    public class ResoSpout : ResoniteMod
    {
        public override string Name => "ResoSpout";
        public override string Author => "kka429";
        public override string Version => "0.0.1";
        public override string Link => "https://github.com/rassi0429/";


        static System.IntPtr _plugin;
        static UnityEngine.Texture2D _sharedTexture;

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.kokoa.saveexif");
            harmony.PatchAll();
            // var ptr = _buffer.GetNativeTexturePtr();
            // Msg(ptr);
            // var eventdata = new EventData(p, _buffer.GetNativeTexturePtr());
            //var _event = new EventKicker(new EventData(p, _buffer.GetNativeTexturePtr()));

            //_sender = new Sender("test3", _buffer);
        }   


        [HarmonyPatch]
        class CameraPatch
        {

            static void SendRenderTexture(UnityEngine.RenderTexture source)
            {
                // Plugin lazy initialization
                if (_plugin == System.IntPtr.Zero)
                {
                    Msg("Spout not ready, creating sender");
                    _plugin = PluginEntry.CreateSender("test", source.width, source.height);
                    if (_plugin == System.IntPtr.Zero)
                    {
                        Msg("spout not ready");
                        return;
                    }// Spout may not be ready.
                }

                // Shared texture lazy initialization
                if (_sharedTexture == null)
                {
                    Msg("sharedTexture is nulll");
                    var ptr = PluginEntry.GetTexturePointer(_plugin);
                    if (ptr != System.IntPtr.Zero)
                    {
                        _sharedTexture = UnityEngine.Texture2D.CreateExternalTexture(
                            PluginEntry.GetTextureWidth(_plugin),
                            PluginEntry.GetTextureHeight(_plugin),
                            UnityEngine.TextureFormat.ARGB32, false, false, ptr
                        );
                        _sharedTexture.hideFlags = HideFlags.DontSave;
                    } else
                    {
                        Msg("ptr is null");
                    }
                }

                // Shared texture update
                if (_sharedTexture != null)
                {
                    var tempRT = UnityEngine.RenderTexture.GetTemporary
                        (_sharedTexture.width, _sharedTexture.height);
                    Graphics.Blit(source, tempRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f));
                    Graphics.CopyTexture(tempRT, _sharedTexture);
                    UnityEngine.RenderTexture.ReleaseTemporary(tempRT);
                }
            }

            //[HarmonyPatch(typeof(CameraRenderEx), "OnPreRender")]
            //[HarmonyPrefix]
            [HarmonyPatch(typeof(CameraRenderEx), "OnPostRender")]
            [HarmonyPrefix]
            static void postfix(CameraRenderEx __instance)
            {
                
                if(__instance.Camera.pixelHeight != 2160)
                {
                    return;
                }

                int num = 2048;
                var tex = new UnityEngine.RenderTexture(num, num, 0);
                tex.dimension = TextureDimension.Cube;
                tex.Create();

                var tmpPosition = __instance.Camera.gameObject.transform.position;
                var tmpTargetTexture = __instance.Camera.targetTexture;


                __instance.Camera.gameObject.transform.position = new Vector3(0, 0, 0);


                var rotation = Quaternion.identity;

                var _material = Resources.Load<UnityEngine.Material>("EquirectangularProjection");
                var shader = _material.shader;
                var newMaterial = new UnityEngine.Material(shader);

                newMaterial.EnableKeyword("FLIP");
                newMaterial.SetTexture("_Cube", tex);
                newMaterial.SetMatrix("_Rotation", Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one));

                UnityEngine.RenderTexture temporary = UnityEngine.RenderTexture.GetTemporary(num, num, 24, tex.format);
                UnityEngine.RenderTexture active = UnityEngine.RenderTexture.active;


                UnityEngine.RenderTexture.active = temporary;
                __instance.Camera.fieldOfView = 90f;
                __instance.Camera.targetTexture = temporary;
                __instance.Camera.transform.eulerAngles = new Vector3(0.0f, -90f, 0.0f);
                Msg(__instance.Camera.transform.eulerAngles.ToString());
                __instance.Camera.Render();
                UnityEngine.Graphics.CopyTexture((UnityEngine.Texture)temporary, 0, 0, (Texture)tex, 0, 0);

                // UnityEngine.RenderTexture temporary2 = UnityEngine.RenderTexture.GetTemporary(num, num, 24, tex.format);
                // __instance.Camera.targetTexture = temporary2;
                __instance.Camera.transform.eulerAngles = new Vector3(0.0f, 90f, 0.0f);
                Msg(__instance.Camera.transform.eulerAngles.ToString());
                __instance.Camera.Render();
                UnityEngine.Graphics.CopyTexture((Texture)temporary, 0, 0, (Texture)tex, 1, 0);

                __instance.Camera.transform.eulerAngles = new Vector3(90f, 180f, 0.0f);
                __instance.Camera.Render();
                UnityEngine.Graphics.CopyTexture((Texture)temporary, 0, 0, (Texture)tex, 2, 0);

                __instance.Camera.transform.eulerAngles = new Vector3(-90f, 180f, 0.0f);
                Msg(__instance.Camera.transform.eulerAngles.ToString());
                __instance.Camera.Render();
                UnityEngine.Graphics.CopyTexture((Texture)temporary, 0, 0, (Texture)tex, 3, 0);

                __instance.Camera.transform.eulerAngles = new Vector3(0.0f, 180f, 0.0f);
                Msg(__instance.Camera.transform.eulerAngles.ToString());
                __instance.Camera.Render();
                UnityEngine.Graphics.CopyTexture((Texture)temporary, 0, 0, (Texture)tex, 4, 0);

                __instance.Camera.transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
                Msg(__instance.Camera.transform.eulerAngles.ToString());
                __instance.Camera.Render();
                UnityEngine.Graphics.CopyTexture((Texture)temporary, 0, 0, (Texture)tex, 5, 0);

                var renderTexture = new UnityEngine.RenderTexture(2048, 1024, 24);
                renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
                Graphics.Blit((Texture)null, renderTexture, newMaterial);

                SendRenderTexture(temporary);

                renderTexture.Release();

                
                UnityEngine.RenderTexture.active = active;
                UnityEngine.RenderTexture.ReleaseTemporary(temporary);
                // UnityEngine.RenderTexture.ReleaseTemporary(temporary2);
                
                if (_plugin != System.IntPtr.Zero)
                    Util.IssuePluginEvent(PluginEntry.Event.Update, _plugin);


                UnityEngine.Object.Destroy((UnityEngine.Object)tex);
                Msg("onPrefix", __instance.Owner);

                __instance.Camera.gameObject.transform.position = tmpPosition;
                __instance.Camera.targetTexture = tmpTargetTexture;

                return;
            }

            [HarmonyPatch(typeof(PostProcessLayer), "OnRenderImage")]
            [HarmonyPostfix]
            static void postfix(PostProcessLayer __instance, UnityEngine.RenderTexture dst)
            {
                if(dst.height != 2160)
                {
                    return;
                }

                var connector = __instance.gameObject.GetComponent<CameraRenderEx>();
                // var camera = __instance.gameObject.GetComponent<UnityEngine.Camera>();

                if (connector != null)
                {
                    var c = connector.Owner;

                    var cameras = connector.Owner.World.RootSlot.GetComponents<FrooxEngine.Camera>();


                    var hoge = cameras.Find(camera => camera.Connector == c);
                    if (hoge != null)
                    {
                        Msg("カメラ発見！");
                    }

                }

                // SendRenderTexture(dst);
                //if (_plugin != System.IntPtr.Zero)
                //    Util.IssuePluginEvent(PluginEntry.Event.Update, _plugin);

                // return true;
            }
        }
    }
}
