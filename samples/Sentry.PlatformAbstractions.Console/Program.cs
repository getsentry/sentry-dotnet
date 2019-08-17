namespace Sentry.PlatformAbstractions.Console
{
    internal static class Program
    {
        private static void Main()
        {
            System.Console.WriteLine($@"Runtime.Current:

    ToString():               {Runtime.Current}

    Name:                     {Runtime.Current.Name}
    Version:                  {Runtime.Current.Version}
    Raw:                      {Runtime.Current.Raw}");

#if NET461
            System.Console.WriteLine($@"
Runtime.Current.FrameworkInstallation:
    ShortName:                {Runtime.Current.FrameworkInstallation?.ShortName}
    Profile:                  {Runtime.Current.FrameworkInstallation?.Profile}
    Version:                  {Runtime.Current.FrameworkInstallation?.Version}
    ServicePack:              {Runtime.Current.FrameworkInstallation?.ServicePack}
    Release:                  {Runtime.Current.FrameworkInstallation?.Release}
    ToString():               {Runtime.Current.FrameworkInstallation}
            ");
#endif

            System.Console.WriteLine($@"Extension methods on Runtime:
    IsMono():                 {Runtime.Current.IsMono()}
    IsNetCore()               {Runtime.Current.IsNetCore()}
    IsNetFx():                {Runtime.Current.IsNetFx()}
");
        }
    }
}
