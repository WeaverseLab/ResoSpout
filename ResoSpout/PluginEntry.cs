// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using System.Runtime.InteropServices;

namespace ResoSpout
{
    static class PluginEntry
    {
        internal enum Event { Update, Dispose }

        internal static bool IsAvailable {
            get {
                return SystemInfo.graphicsDeviceType ==
                    UnityEngine.Rendering.GraphicsDeviceType.Direct3D11;
            }
        }

        [DllImport(@"KlakSpout.dll")]
        internal static extern System.IntPtr GetRenderEventFunc();

        [DllImport(@"KlakSpout.dll")]
        internal static extern System.IntPtr CreateSender(string name, int width, int height);

        [DllImport(@"KlakSpout.dll")]
        internal static extern System.IntPtr CreateReceiver(string name);

        [DllImport(@"KlakSpout.dll")]
        internal static extern System.IntPtr GetTexturePointer(System.IntPtr ptr);

        [DllImport(@"KlakSpout.dll")]
        internal static extern int GetTextureWidth(System.IntPtr ptr);

        [DllImport(@"KlakSpout.dll")]
        internal static extern int GetTextureHeight(System.IntPtr ptr);


        [DllImport(@"KlakSpout.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CheckValid(System.IntPtr ptr);


        [DllImport(@"KlakSpout.dll")]
        internal static extern int ScanSharedObjects();


        [DllImport(@"KlakSpout.dll")]
        internal static extern System.IntPtr GetSharedObjectName(int index);

        internal static string GetSharedObjectNameString(int index)
        {
            var ptr = GetSharedObjectName(index);
            return ptr != System.IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }

    }
}
