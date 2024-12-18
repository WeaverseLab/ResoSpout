﻿using CppSharp.AST;
using Elements.Assets;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityFrooxEngineRunner;
using uOSC;

namespace ResoSpout
{
    public class ResoSpout : ResoniteMod
    {
        public override string Name => "ResoSpout";
        public override string Author => "kka429";
        public override string Version => "0.0.1";
        public override string Link => "https://github.com/rassi0429/";

        static int[] allowedHeight = { 721, 722, 723, 724 };

        // Separate dictionaries for plugins and shared textures
        static Dictionary<string, IntPtr> plugins = new Dictionary<string, IntPtr>();
        static Dictionary<string, UnityEngine.Texture2D> sharedTextures = new Dictionary<string, UnityEngine.Texture2D>();
        static Dictionary<string, UnityEngine.RenderTexture> tmpTextures = new Dictionary<string, UnityEngine.RenderTexture>();

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.kokoa.resospout");
            harmony.PatchAll();

            Engine.Current.RunPostInit(() =>
            {
                Msg("RunPostInit");
                Engine.Current.WorldManager.WorldAdded += (World w) =>
                {
                    Msg("world focused");
                };
            });
        }

        static string getNameFromTextureResolution(int width, int height)
        {
            return width.ToString() + "x" + height.ToString();
        }

        static IntPtr GetOrCreatePlugin(int width, int height)
        {
            string key = getNameFromTextureResolution(width, height);

            if (!plugins.ContainsKey(key))
            {
                Msg($"Creating new plugin for resolution: {key}");
                IntPtr plugin = PluginEntry.CreateSender(key, width, height);
                if (plugin == IntPtr.Zero)
                {
                    Msg("Failed to create Spout sender");
                    return IntPtr.Zero;
                }

                plugins.Add(key, plugin);
                Msg($"Created new plugin for resolution: {key}");
            }

            return plugins[key];
        }

        static UnityEngine.Texture2D GetOrCreateSharedTexture(IntPtr plugin)
        {
            if (plugin == IntPtr.Zero)
            {
                return null;
            }

            // get key from plugins
            string key = plugins.FirstOrDefault(x => x.Value == plugin).Key;

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
                IntPtr plugin = GetOrCreatePlugin(source.width, source.height);
                Util.IssuePluginEvent(PluginEntry.Event.Update, plugin);
                UnityEngine.Texture2D sharedTexture = GetOrCreateSharedTexture(plugin);



                // UnityEngine.Texture2D tmpTexture = new UnityEngine.Texture2D(source.width, source.height, UnityEngine.TextureFormat.ARGB32, false);

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

            [HarmonyPatch(typeof(PostProcessLayer), "OnRenderImage")]
            [HarmonyPrefix]
            static void _postfix(PostProcessLayer __instance, UnityEngine.RenderTexture src, UnityEngine.RenderTexture dst)
            {
                if (!allowedHeight.Contains(src.height))
                {
                    return;
                }
                // Msg("OnRenderImage");

                SendRenderTexture(src);
                Graphics.Blit(src, dst);

                foreach (var p in plugins)
                {
                    Util.IssuePluginEvent(PluginEntry.Event.Update, p.Value);
                }
            }
        }
    }
}
