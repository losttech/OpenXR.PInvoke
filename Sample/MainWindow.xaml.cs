namespace Sample {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using OpenXR.PInvoke;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            this.InitializeComponent();
        }

        void OnLoaded(object sender, RoutedEventArgs e) {
            NativeLibrary.SetDllImportResolver(typeof(XR).Assembly, Resolver);
            string[] extensions = SelectExtensions();
        }

        static unsafe XrExtensionProperties[] GetExtensionProperties() {
            uint extensionCount;
            Check(XR.EnumerateInstanceExtensionProperties(null, 0, &extensionCount, null));
            var properties = new XrExtensionProperties[checked((int)extensionCount)];
            for (int extension = 0; extension < extensionCount; extension++)
                properties[extension] = new XrExtensionProperties {
                    type = XrStructureType.XR_TYPE_EXTENSION_PROPERTIES,
                };
            fixed(XrExtensionProperties* ptr = properties)
                Check(XR.EnumerateInstanceExtensionProperties(null, extensionCount, &extensionCount, ptr));
            return properties;
        }

        static unsafe string[] SelectExtensions() {
            XrExtensionProperties[] properties = GetExtensionProperties();
            string[] names = properties.Select(p => new string(p.extensionName)).ToArray();

            var enable = new List<string>();
            bool EnableExtensionIfSupported(string extensionName) {
                if (names.Contains(extensionName)) {
                    enable.Add(extensionName);
                    return true;
                }
                return false;
            }

            if (!EnableExtensionIfSupported("XR_KHR_D3D11_enable"))
                throw new PlatformNotSupportedException("XR_KHR_D3D11_enable");

            return enable.ToArray();
        }

        static void Check(XrResult result) {
            if (result != XrResult.XR_SUCCESS)
                throw new Exception(result.ToString());
        }

        static IntPtr Resolver(string libraryname, Assembly assembly, DllImportSearchPath? searchpath) {
            if (libraryname != "openxr_loader") return IntPtr.Zero;

            string installDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            string bitness = IntPtr.Size switch {
                4 => "Win32",
                8 => "x64",
                _ => throw new PlatformNotSupportedException(),
            };

            string path = Path.Combine(new[] {
                installDir, "native", bitness, "release", "bin",
                Path.ChangeExtension(libraryname, ".dll"),
            });

            return NativeLibrary.Load(path);
        }
    }
}
