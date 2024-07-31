using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityFrooxEngineRunner;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Threading.Tasks;
using System.Resources;

namespace ResoSpout
{

    class WSHandler : WebSocketBehavior
    {
        public delegate void ExternalOnMessage(string message);
        public event ExternalOnMessage externalOnMessage = delegate { };

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            externalOnMessage(e.Data);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            ResoniteMod.Msg("opened!");
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            base.OnError(e);
            ResoniteMod.Msg("errored! reason:" + e.Message + " exception: " + e.Exception);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            ResoniteMod.Msg("closed! reason: " + e.Reason + " code: " + e.Code + " wasClean: " + e.WasClean);
        }
    }

    public class KOKOPILoader : MonoBehaviour
    {

        // [DllImport(@"C:\Users\neo\Downloads\NeosSR-master\Assets\SRDisplayUnityPlugin\Plugins\x86_64\xr_runtime_unity_wrapper.dll", EntryPoint = "srd_xr_StartSession")]
        // public static extern int StartSession();

        void Start()
        {
            ResoniteMod.Msg("KOKOPI UnityLoader started");

            var myLoadedAssetBundle = AssetBundle.LoadFromFile(@"C:\Program Files (x86)\Steam\steamapps\common\Resonite\AssetBundle\homography");
            if (myLoadedAssetBundle == null)
            {
                ResoniteMod.Msg("Failed to load AssetBundle!");
                return;
            }

            ResoniteMod.Msg(myLoadedAssetBundle.GetAllAssetNames());

            //var assets = myLoadedAssetBundle.LoadAllAssets();
            var nebbia = myLoadedAssetBundle.LoadAsset("assets/nebbia/nebbia_2.prefab");
            var liltoon = myLoadedAssetBundle.LoadAsset<UnityEngine.Shader>("assets/liltoon/shader/lts.shader");
            var liltoonMat = new UnityEngine.Material(liltoon);

            var nebbiaObj = Instantiate(nebbia) as GameObject;
            nebbiaObj.transform.parent = this.transform;
            nebbiaObj.transform.localPosition = new UnityEngine.Vector3(0.0f, 0.0f, 0.0f);
            

            //// show all children
            //foreach (Transform child in nebbiaObj.transform)
            //{
            //    Console.WriteLine(child.name);
            //}
            // show all components name in the children
            foreach (Transform child in nebbiaObj.transform)
            {
                Console.WriteLine(child.name);
                foreach (UnityEngine.Component component in child.GetComponents<UnityEngine.Component>())
                {
                    Console.WriteLine(component.GetType());
                    if(component.GetType() == typeof(UnityEngine.SkinnedMeshRenderer))
                    {
                        var s = component as UnityEngine.SkinnedMeshRenderer;
                        Console.WriteLine(s.sharedMesh.vertexCount);
                        Console.WriteLine(s.materials.Length);
                        // Console.WriteLine()
                        Console.WriteLine(s.material.shader.name);
                        UnityEngine.Texture tex = s.material.mainTexture;
                        Console.WriteLine(tex);
                        // var ma = new UnityEngine.Material(UnityEngine.Shader.Find("Standard"));
                        // ma.mainTexture = tex;
                        // s.material = ma;
                    }
                }
            }


            // var s = nebbiaObj.gameObject.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
            // Console.WriteLine(s);

            // var m = myLoadedAssetBundle.LoadAsset("assets/nebbia/new folder/body.mesh");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.GetComponent<UnityEngine.MeshRenderer>().material = liltoonMat;
            // mesh.GetComponent<UnityEngine.MeshFilter>().mesh = m as UnityEngine.Mesh;
            mesh.transform.parent = this.transform;
           

            // Console.WriteLine(mesh.transform.name);

            Console.WriteLine("Hello World!");
            //int result = StartSession();
            //Console.WriteLine("Result: " + result);
        }

        // Update is called once per frame
        void Update()
        {

        }

        void LateUpdate()
        {

        }
    }


    public class ResoSpout : ResoniteMod
    {
        public override string Name => "ResoSpout";
        public override string Author => "kka429";
        public override string Version => "0.0.1";
        public override string Link => "https://github.com/rassi0429/";

        static int[] allowedSenderHeight = { 1081, 1082, 1083, 2161, 2162 };

        // Separate dictionaries for plugins and shared textures
        static Dictionary<string, IntPtr> senderPlugins = new Dictionary<string, IntPtr>();
        static Dictionary<string, UnityEngine.Texture2D> sharedTextures = new Dictionary<string, UnityEngine.Texture2D>();
        static Dictionary<string, UnityEngine.RenderTexture> tmpTextures = new Dictionary<string, UnityEngine.RenderTexture>();

        static Dictionary<string, IntPtr> recieverPlugins = new Dictionary<string, IntPtr>();
        static Dictionary<string, UnityEngine.Texture2D> recieverTextures = new Dictionary<string, UnityEngine.Texture2D>();
        static Dictionary<string, int> recieverHeight = new Dictionary<string, int>();

        static float leftEyeAspect = 1;
        static float rightEyeAspect = 1;

        static Matrix4x4 leftEyeProjection = new Matrix4x4();
        static Matrix4x4 rightEyeProjection = new Matrix4x4();

        static float leftEyeFOV = 30.28924f;
        static float rightEyeFOV = 30.28924f;

        static Elements.Core.float3 watcherPosition = new  Elements.Core.float3();
        static Elements.Core.float3 leftEyePosition = new  Elements.Core.float3();
        static Elements.Core.float3 rightEyePosition = new Elements.Core.float3();

        public static UnityEngine.Material _HomographyMaterial;

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.kokoa.resospout");
            harmony.PatchAll();

            Engine.Current.RunPostInit(() =>
            {
                Msg("RunPostInit");
                // GetOrCreateReceiverPlugin("OBS1", 721); 
                // GetOrCreateReceiverPlugin("OBS2", 722);
                // GetOrCreateReceiverPlugin("OBS3", 723);
                Engine.Current.WorldManager.WorldAdded += (World w) =>
                {
                    Msg("world focused");
                    w.WorldRunning += (World _) =>
                    {
                        Msg("world running");
                    };  
                };
            });

            // get active scene
            // var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            //Msg("scene name: " + scene.name);
            // scene.GetRootGameObjects().ToList()[0].AddComponent<KOKOPILoader>();/


            //var myLoadedAssetBundle = AssetBundle.LoadFromFile(@"C:\Program Files (x86)\Steam\steamapps\common\Resonite\AssetBundle\homography");
            //if (myLoadedAssetBundle == null)
            //{
            //    Msg("Failed to load AssetBundle!");
            //    return;
            //}

            //Msg(myLoadedAssetBundle.GetAllAssetNames());
            //var image = myLoadedAssetBundle.LoadAsset("assets/srdisplayunityplugin/resources/lena.png");
            //Msg(image);
            // if (shader == null)
            //{
            //    Msg("Failed to load shader!");
            //    return;
            //}


            WebSocketServer ws = new WebSocketServer(8887);
            ws.AddWebSocketService<WSHandler>("/", x =>
            {
                x.externalOnMessage += (string message) =>
                {
                    new Task(() =>
                    {
                        // Msg("received message: " + message);
                        string[] strings = message.Split(',');
                        /*
                            EyeAspect,Left,1.777779
                            EyeFOV,Left,30.28924
                            ProjectionMatrix,Left, 2.034649, 0, -0.3118973, 0, 0, 3.617155, 3.024909, 0, 0, 0, -1.00001, -0.0100001, 0, 0, -1, 0
                        */
                        switch (strings)
                        {
                            case string[] s when s[0] == "EyeAspect":
                                if (s[1] == "Left")
                                {
                                    leftEyeAspect = float.Parse(s[2]);
                                }
                                else if (s[1] == "Right")
                                {
                                    rightEyeAspect = float.Parse(s[2]);
                                }
                                break;
                            case string[] s when s[0] == "ProjectionMatrix":
                                if (s[1] == "Left")
                                {
                                    leftEyeProjection = new Matrix4x4();
                                    leftEyeProjection.m00 = float.Parse(s[2]);
                                    leftEyeProjection.m01 = float.Parse(s[3]);
                                    leftEyeProjection.m02 = float.Parse(s[4]);
                                    leftEyeProjection.m03 = float.Parse(s[5]);
                                    
                                    leftEyeProjection.m10 = float.Parse(s[6]);
                                    leftEyeProjection.m11 = float.Parse(s[7]);
                                    leftEyeProjection.m12 = float.Parse(s[8]);
                                    leftEyeProjection.m13 = float.Parse(s[9]);

                                    leftEyeProjection.m20 = float.Parse(s[10]);
                                    leftEyeProjection.m21 = float.Parse(s[11]);
                                    leftEyeProjection.m22 = float.Parse(s[12]);
                                    leftEyeProjection.m23 = float.Parse(s[13]);

                                    leftEyeProjection.m30 = float.Parse(s[14]);
                                    leftEyeProjection.m31 = float.Parse(s[15]);
                                    leftEyeProjection.m32 = float.Parse(s[16]);
                                    leftEyeProjection.m33 = float.Parse(s[17]);

                                }
                                else if (s[1] == "Right")
                                {
                                    rightEyeProjection = new Matrix4x4();
                                    rightEyeProjection.m00 = float.Parse(s[2]);
                                    rightEyeProjection.m01 = float.Parse(s[3]);
                                    rightEyeProjection.m02 = float.Parse(s[4]);
                                    rightEyeProjection.m03 = float.Parse(s[5]);
                                        
                                    rightEyeProjection.m10 = float.Parse(s[6]);
                                    rightEyeProjection.m11 = float.Parse(s[7]);
                                    rightEyeProjection.m12 = float.Parse(s[8]);
                                    rightEyeProjection.m13 = float.Parse(s[9]);

                                    rightEyeProjection.m20 = float.Parse(s[10]);
                                    rightEyeProjection.m21 = float.Parse(s[11]);
                                    rightEyeProjection.m22 = float.Parse(s[12]);
                                    rightEyeProjection.m23 = float.Parse(s[13]);

                                    rightEyeProjection.m30 = float.Parse(s[14]);
                                    rightEyeProjection.m31 = float.Parse(s[15]);
                                    rightEyeProjection.m32 = float.Parse(s[16]);
                                    rightEyeProjection.m33 = float.Parse(s[17]);
                                }
                                break;
                            case string[] s when s[0] == "EyeFOV":
                                if (s[1] == "Left")
                                {
                                    leftEyeFOV = float.Parse(s[2]);
                                }
                                else if (s[1] == "Right")
                                {
                                    rightEyeFOV = float.Parse(s[2]);
                                }
                                break;
                            case string[] s when s[0] == "Watcher":
                                watcherPosition = new Elements.Core.float3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                                break;
                            case string[] s when s[0] == "Left":
                                leftEyePosition = new Elements.Core.float3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                                break;
                            case string[] s when s[0] == "Right":
                                rightEyePosition = new Elements.Core.float3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                                break;

                        }
                    }).Start();

                };
            });
            ws.KeepClean = false;
            ws.Start();
            Msg("websocket server started on localhost:8887");
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
            static void Postfix(CameraRenderEx __instance)
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
                

                if(tempRenderTexture.height == 2161)
                {
                    var child = new GameObject();
                    child.transform.parent = __instance.gameObject.transform;
                    child.transform.localPosition = new UnityEngine.Vector3(0.0f, 0.0f, 0.0f);
                    child.transform.localRotation = UnityEngine.Quaternion.identity;
                    var _c = child.gameObject.AddComponent<UnityEngine.Camera>();

                    // Msg(_c);

                    // left eye
                    // var shaderTmpTexture = UnityEngine.RenderTexture.GetTemporary(tempRenderTexture.width, tempRenderTexture.height, 24);
                    // Msg(cam.transform.localPosition);
                    // Msg(cam.transform.rotation);
                    // Msg(cam.transform.localScale);

                    // var _pos = cam.transform.position;
                    // var _rot = cam.transform.rotation;

                    // cam.transform.position = new UnityEngine.Vector3(0.0f, 0.0f, 0.0f);
                    // cam.transform.rotation = UnityEngine.Quaternion.identity;
                    _c.targetTexture = tempRenderTexture;

                    // cam.ResetProjectionMatrix();

                    // _c.fieldOfView = 140;
                    // _c.aspect = leftEyeAspect;
                    // Msg(_c.fieldOfView);

                    // _c.usePhysicalProperties = false;
                    //leftEyeProjection.m23 = -0.01f;
                    //leftEyeProjection.m32 = -1.0f;
                    _c.projectionMatrix = leftEyeProjection;

                    // Msg(_c.projectionMatrix);
                    _c.Render();

                    SendRenderTexture(tempRenderTexture);

                    UnityEngine.Object.Destroy(child);

                    // cam.transform.position = _pos;
                    // cam.transform.rotation = _rot;

                    // UnityEngine.RenderTexture.ReleaseTemporary(shaderTmpTexture);
                } else if (tempRenderTexture.height == 2162)
                {
                    // right eye
                    //var shaderTmpTexture = UnityEngine.RenderTexture.GetTemporary(tempRenderTexture.width, tempRenderTexture.height, 24);
                    //cam.targetTexture = shaderTmpTexture;
                    //cam.ResetProjectionMatrix();
                    //cam.aspect = rightEyeAspect;
                    //cam.projectionMatrix = rightEyeProjection;
                    //cam.Render();
                    //Graphics.Blit(shaderTmpTexture, tempRenderTexture, _HomographyMaterial);
                    //UnityEngine.RenderTexture.ReleaseTemporary(shaderTmpTexture);
                    var child = new GameObject();
                    child.transform.parent = __instance.gameObject.transform;
                    child.transform.localPosition = new UnityEngine.Vector3(0.0f, 0.0f, 0.0f);
                    child.transform.localRotation = UnityEngine.Quaternion.identity;
                    var _c = child.gameObject.AddComponent<UnityEngine.Camera>();

                    // Msg(_c);

                    // left eye
                    // var shaderTmpTexture = UnityEngine.RenderTexture.GetTemporary(tempRenderTexture.width, tempRenderTexture.height, 24);
                    // Msg(cam.transform.localPosition);
                    // Msg(cam.transform.rotation);
                    // Msg(cam.transform.localScale);

                    // var _pos = cam.transform.position;
                    // var _rot = cam.transform.rotation;

                    // cam.transform.position = new UnityEngine.Vector3(0.0f, 0.0f, 0.0f);
                    // cam.transform.rotation = UnityEngine.Quaternion.identity;
                    _c.targetTexture = tempRenderTexture;

                    // cam.ResetProjectionMatrix();

                    // _c.fieldOfView = 140;
                    // _c.aspect = leftEyeAspect;
                    // Msg(_c.fieldOfView);

                    // _c.usePhysicalProperties = false;
                    //leftEyeProjection.m23 = -0.01f;
                    //leftEyeProjection.m32 = -1.0f;
                    _c.projectionMatrix = rightEyeProjection;

                    // Msg(_c.projectionMatrix);
                    _c.Render();

                    SendRenderTexture(tempRenderTexture);

                    UnityEngine.Object.Destroy(child);
                } else
                {
                    cam.targetTexture = tempRenderTexture;
                    cam.nearClipPlane = 0.01f;
                    cam.Render();
                }
                


                cam.targetTexture = tmpCameraRenderTexture;

                RenderHelper.BeginRenderContext(_prevContext.Value);
            }

            [HarmonyPatch(typeof(FrooxEngine.Engine), "RunUpdateLoop")]
            class Patch
            {
                static bool Prefix(FrooxEngine.Engine __instance)
                {
                    if (__instance.WorldManager.FocusedWorld == null) return true;

                    __instance.WorldManager.FocusedWorld.RunSynchronously(() =>
                    {
                        // Msg("FindChild: " + __instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDWatcher"));
                        if(__instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDWatcher") != null)
                        {
                            __instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDWatcher").LocalPosition = watcherPosition;
                        }
                        if(__instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDLeftEye") != null)
                        {
                            __instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDLeftEye").LocalPosition = leftEyePosition;
                        }
                        if(__instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDRightEye") != null)
                        {
                            __instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDRightEye").LocalPosition = rightEyePosition;
                        }

                        // __instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDLeftEye").LocalPosition = leftEyePosition;
                        // __instance.WorldManager.FocusedWorld.RootSlot.FindChildInHierarchy("#SRDRightEye").LocalPosition = rightEyePosition;
                    });

                    return true;
                }
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

                // Msg(src.height);
                if (!allowedSenderHeight.Contains(src.height))
                {
                    return true;
                }

                var key = Util.getNameFromTextureResolution(src.width, src.height);
                if (tmpTextures.ContainsKey(key))
                {
                    var tex = tmpTextures[key];
                    // SendRenderTexture(tex);
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
