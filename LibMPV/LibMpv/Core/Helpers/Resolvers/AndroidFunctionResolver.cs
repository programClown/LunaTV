// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace HanumanInstitute.LibMpv.Core;

public class AndroidFunctionResolver : FunctionResolverBase
{
    private const string Libdl = "libdl.so";
    private const int RTLD_NOW = 0x002;

    // Android doesn't support versioned .so names (e.g. libmpv.so.2).
    // Always load as libmpv.so regardless of version.
    protected override string GetNativeLibraryName(string libraryName, int version) =>
        $"{libraryName}.so";
    protected override string[] GetSearchPaths() => new string[] { "" }; // Let the system determine where libmpv is
    protected override IntPtr LoadNativeLibrary(string libraryName) => dlopen(libraryName, RTLD_NOW);
    protected override IntPtr FindFunctionPointer(IntPtr nativeLibraryHandle, string functionName) => dlsym(nativeLibraryHandle, functionName);

    [DllImport(Libdl)]
    public static extern IntPtr dlsym(IntPtr handle, string symbol);

    [DllImport(Libdl)]
    public static extern IntPtr dlopen(string fileName, int flag);
}
