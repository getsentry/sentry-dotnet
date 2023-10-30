#if !__MOBILE__
public static class CommonModuleInit
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The \'ModuleInitializer\' attribute should not be used in libraries")]
    public static void Init()
    {
        // TODO: Do we need this anymore? We can't use XUnit for AOT Verify tests anyway...
        // PathInfo DeriveAotPathInfo(string sourceFile, string projectDirectory, Type type, MethodInfo method)
        // {
        //     var uniqueForAot = method.GetCustomAttribute<UniqueForAotAttribute>()
        //                     ?? type.GetCustomAttribute<UniqueForAotAttribute>();
        //     var derivedMethodName = (uniqueForAot != null)
        //         ? method.Name + ".AOT"
        //         : method.Name;
        //     return new PathInfo(
        //         directory: projectDirectory,
        //         typeName: type.Name,
        //         // This ensures a unique name for our verify files when trimming is enabled
        //         methodName: derivedMethodName
        //     );
        // }
        //
        // Verifier.DerivePathInfo(DeriveAotPathInfo);

        VerifyDiffPlex.Initialize();
    }
}
#endif
