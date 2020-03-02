## Getting started on Windows

1. Create a new *.NET Core* WPF or Windows Forms project
1. Add a reference to OpenXR.PInvoke package
2. Allow unsafe code:
```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```
3. Reference OpenXR.Loader package
4. Modify `.csproj` file to have these (works on Windows only)
```xml
<ItemGroup>
  <PackageReference Include="OpenXR.Loader" Version="1.0.6.2" GeneratePathProperty="true" />
</ItemGroup>

<ItemGroup>
  <OpenXRBinaries Include="$(PkgOpenXR_Loader)/native/**/bin/openxr_loader.*" />
</ItemGroup>

<Target Name="CopyOpenXRLoaderFiles" AfterTargets="Build">
  <Copy SourceFiles="@(OpenXRBinaries)" DestinationFiles="@(OpenXRBinaries->'$(OutDir)native\%(RecursiveDir)%(Filename)%(Extension)')" />
</Target>
```
5. Add a custom native library resolver (works as is on Windows only):
```csharp
NativeLibrary.SetDllImportResolver(typeof(OpenXR.PInvoke.XR).Assembly, Resolver);

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
```