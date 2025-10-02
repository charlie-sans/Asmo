#define USE_OPENXR
using System;
using System.Runtime.InteropServices;
// Attempt to use Evergine OpenXR bindings; guard with conditional if needed.
// using static Evergine.Bindings.OpenXR.NativeLibrary.
#if USE_OPENXR
using Evergine.Bindings.OpenXR;
#endif

namespace Asmo.Window
{
    public class VrManager : IDisposable
    {
        public bool Initialized { get; private set; }

        // Core OpenXR handles (wrapped in conditional compilation)
#if USE_OPENXR
    private XrInstance _instance;
    private ulong _systemId;
    private XrSession _session;
    private readonly XrFormFactor _formFactor = XrFormFactor.XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY;
    private readonly XrViewConfigurationType _viewType = XrViewConfigurationType.XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO;
#else
        private object? _instance;
        private object? _systemId;
        private object? _session;
#endif

        private bool _frameBegun = false;

        public bool InitOpenXR_OpenGL()
        {
            if (Initialized) return true;
            // Full initialization only when OpenXR is enabled via compilation symbol
#if USE_OPENXR
            try
            {
                if (!CreateInstance()) { Console.WriteLine("[VR] CreateInstance failed (no instance). Fallback to non-VR."); return false; }
                if (!GetSystem()) { Console.WriteLine("[VR] xrGetSystem failed. Fallback to non-VR."); return false; }
                if (!CreateSession_OpenGL()) { Console.WriteLine("[VR] Session creation failed (OpenGL binding or runtime incomplete). Fallback."); return false; }
                Initialized = true;
                Console.WriteLine("[VR] Initialization succeeded.");
                return true;
            }
            catch (DllNotFoundException dllEx)
            {
                Console.WriteLine("[VR] OpenXR loader DLL not found: " + dllEx.Message + "\nInstall an OpenXR runtime (SteamVR, WMR, Oculus) or place openxr_loader.dll beside the executable.");
                return false;
            }
            catch (TypeInitializationException tie) when (tie.InnerException is InvalidOperationException ioe && ioe.Message.Contains("openxr_loader.dll", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[VR] OpenXR native loader missing (TypeInitializationException). Install / add openxr_loader.dll. Fallback.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VR] Unexpected VR init exception: " + ex.GetType().Name + " - " + ex.Message);
                return false;
            }
#else
            // Not compiled with OpenXR support yet
            return false;
#endif
        }

#if USE_OPENXR
        private unsafe bool CreateInstance()
        {
            static ulong MakeVersion(uint maj, uint min, uint pat) => ((ulong)maj << 48) | ((ulong)min << 32) | pat;

            // 1. Enumerate extensions
            uint extCount = 0;
            var resCount = OpenXRNative.xrEnumerateInstanceExtensionProperties((byte*)0, 0, &extCount, (XrExtensionProperties*)0);
            if (resCount != XrResult.XR_SUCCESS || extCount == 0)
            {
                Console.WriteLine("[VR] No OpenXR extensions enumerated.");
                return false;
            }
            var extProps = new XrExtensionProperties[extCount];
            for (int i = 0; i < extProps.Length; i++) extProps[i].type = XrStructureType.XR_TYPE_EXTENSION_PROPERTIES;
            fixed (XrExtensionProperties* pExt = extProps)
            {
                resCount = OpenXRNative.xrEnumerateInstanceExtensionProperties((byte*)0, extCount, &extCount, pExt);
            }
            if (resCount != XrResult.XR_SUCCESS)
            {
                Console.WriteLine($"[VR] Failed to enumerate extension properties: {resCount}");
                return false;
            }

            bool Has(string name)
            {
                for (int i = 0; i < extProps.Length; i++)
                {
                    fixed (byte* pName = extProps[i].extensionName)
                    {
                        var nm = GetCString(pName, 128);
                        if (nm == name) return true;
                    }
                }
                return false;
            }

            // Required extension for OpenGL
            string[] desired = new[]{"XR_KHR_opengl_enable"};
            int extEnableCount = desired.Length;
            IntPtr* extNamePtrs = stackalloc IntPtr[extEnableCount];
            // Keep managed arrays alive in this scope
            var managedExtArrays = new byte[extEnableCount][];
            for (int ei = 0; ei < desired.Length; ei++)
            {
                string d = desired[ei];
                if (!Has(d)) { Console.WriteLine($"[VR] Missing required extension {d}"); return false; }
                managedExtArrays[ei] = System.Text.Encoding.UTF8.GetBytes(d + "\0");
                fixed (byte* p = managedExtArrays[ei])
                {
                    extNamePtrs[ei] = (IntPtr)p;
                }
            }

            try
            {
                XrApplicationInfo appInfo = new XrApplicationInfo();
                string name = "ASMO";
                for (int i = 0; i < name.Length && i < 128 - 1; i++) appInfo.applicationName[i] = (byte)name[i];
                appInfo.applicationVersion = 1;
                appInfo.engineVersion = 1;
                appInfo.apiVersion = MakeVersion(1, 0, 0);

                XrInstanceCreateInfo createInfo = new XrInstanceCreateInfo();
                createInfo.type = XrStructureType.XR_TYPE_INSTANCE_CREATE_INFO;
                createInfo.applicationInfo = appInfo;
                createInfo.enabledExtensionCount = (uint)extEnableCount;
                createInfo.enabledExtensionNames = (byte**)extNamePtrs;
                XrInstance localInstance = default;
                var res = OpenXRNative.xrCreateInstance(&createInfo, &localInstance);
                if (res != XrResult.XR_SUCCESS)
                {
                    Console.WriteLine($"[VR] xrCreateInstance failed: {res}");
                    return false;
                }
                _instance = localInstance;
                Console.WriteLine("[VR] OpenXR instance created.");
                return true;
            }
            finally { }

            static string GetCString(byte* src, int max) { int len = 0; while (len < max && src[len] != 0) len++; return System.Text.Encoding.UTF8.GetString(src, len); }
        }

        private unsafe bool GetSystem()
        {
            if (_instance.Handle == 0) return false;
            XrSystemGetInfo sysInfo = new XrSystemGetInfo();
            sysInfo.type = XrStructureType.XR_TYPE_SYSTEM_GET_INFO;
            sysInfo.formFactor = _formFactor;
            ulong systemId = 0;
            var res = OpenXRNative.xrGetSystem(_instance, &sysInfo, &systemId);
            if (res != XrResult.XR_SUCCESS) return false;
            _systemId = systemId;
            return true;
        }

    private unsafe bool CreateSession_OpenGL()
        {
            // High-level plan (scaffold):
            // 1. Acquire Win32 HDC and current HGLRC (wglGetCurrentContext)
            // 2. Fill XrGraphicsBindingOpenGLWin32KHR
            // 3. Create XrSession via xrCreateSession
            // 4. Enumerate view config + create swapchains (NOT IMPLEMENTED HERE)
            // NOTE: This scaffold returns false so caller falls back to non‑VR until fully implemented.

#if OPENXR_GL_IMPLEMENTED
            if (_systemId == 0 || _instance.Handle == 0)
                return false;

            if (!GetWin32OpenGLBinding(out var binding))
                return false;

            XrSessionCreateInfo sci = new XrSessionCreateInfo();
            sci.type = XrStructureType.XR_TYPE_SESSION_CREATE_INFO;
            sci.systemId = _systemId;
            // chain graphics binding
            XrGraphicsBindingOpenGLWin32KHR* pBind = &binding;
            sci.next = pBind;
            XrSession sessionLocal = default;
            var resSess = OpenXRNative.xrCreateSession(_instance, &sci, &sessionLocal);
            if (resSess != XrResult.XR_SUCCESS)
                return false;
            _session = sessionLocal;
            return true; // with symbol enabled we succeed
#else
            return false; // not implemented yet
#endif
        }

#if OPENXR_GL_IMPLEMENTED && USE_OPENXR
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct XrGraphicsBindingOpenGLWin32KHR
        {
            public XrStructureType type;
            public void* next;
            public nint hDC;
            public nint hGLRC;
        }

        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("opengl32.dll")] private static extern IntPtr wglGetCurrentContext();

        private unsafe bool GetWin32OpenGLBinding(out XrGraphicsBindingOpenGLWin32KHR binding)
        {
            binding = new XrGraphicsBindingOpenGLWin32KHR
            {
                type = (XrStructureType)1000063000, // XR_TYPE_GRAPHICS_BINDING_OPENGL_WIN32_KHR (value per spec)
                next = null,
                hDC = IntPtr.Zero,
                hGLRC = IntPtr.Zero
            };
            try
            {
                IntPtr hwnd = GetActiveWindow();
                if (hwnd == IntPtr.Zero) return false;
                IntPtr hdc = GetDC(hwnd);
                IntPtr glrc = wglGetCurrentContext();
                if (hdc == IntPtr.Zero || glrc == IntPtr.Zero) return false;
                binding.hDC = hdc;
                binding.hGLRC = glrc;
                return true;
            }
            catch { return false; }
        }
#endif
#endif

        public void BeginFrame()
        {
            if (!Initialized) return;
            if (_frameBegun) return;
            // TODO: xrWaitFrame + xrBeginFrame (when OpenXR enabled)
            _frameBegun = true;
        }

        public void SubmitQuadLayer(int glTexture, int width, int height)
        {
            if (!Initialized) return;
            // TODO: build + store quad layer referencing the supplied texture (when OpenXR enabled)
        }

        public void EndFrame()
        {
            if (!Initialized) return;
            if (!_frameBegun) return;
            // TODO: xrEndFrame with submitted layers (when OpenXR enabled)
            _frameBegun = false;
        }

        public void Dispose()
        {
            if (!Initialized) return;
            // TODO: destroy session then instance (when OpenXR enabled)
            Initialized = false;
        }
    }
}
