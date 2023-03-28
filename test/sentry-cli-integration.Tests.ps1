Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# In CI, the module is loaded automatically
if (!(Test-Path env:CI ))
{
    Import-Module $PSScriptRoot/../../github-workflows/sentry-cli/integration-test/action.psm1 -Force
}

BeforeAll {
    function DotnetBuild([string]$Sample)
    {
        $rootDir = "$(Get-Item $PSScriptRoot/../../)"

        Invoke-SentryServer {
            Param([string]$url)
            Write-Host "Building $Sample"
            dotnet build "samples/$sample/$sample.csproj" -c Release --no-restore --nologo `
                /p:UseSentryCLI=true `
                /p:SentryOrg=org `
                /p:SentryProject=project `
                /p:SentryUrl=$url `
                /p:SentryAuthToken=dummy `
            | ForEach-Object {
                if ($_ -match "^Time Elapsed ")
                {
                    "Time Elapsed [value removed]"
                }
                elseif ($_ -match "\[[0-9/]+\]")
                {
                    # Skip lines like `[102/103] Sentry.Samples.Maui.dll -> Sentry.Samples.Maui.dll.so`
                }
                else
                {
                    "$_". `
                        Replace($rootDir, '').  `
                        Replace('\', '/')
                }
            }
            | ForEach-Object {
                Write-Host "  $_"
                $_
            }
        }
    }
}

Describe 'CLI-integration' {
    It "uploads symbols for a console app build" {
        $result = DotnetBuild 'Sentry.Samples.Console.Basic'
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('apphost.exe', 'Sentry.pdb', 'Sentry.Samples.Console.Basic.pdb')
    }

    It "uploads symbols for a MAUI app build" {
        $result = DotnetBuild 'Sentry.Samples.Maui'
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $actual = $result.UploadedDebugFiles() | Sort-Object -Unique
        $expected = @(
            'apphost.exe', `
                'Java.Interop.dll.so', `
                'K4os.Compression.LZ4.dll.so', `
                'libmonodroid.so', `
                'libmonosgen-2.0.so', `
                'libsentry-android.so', `
                'libsentry.so', `
                'libsentrysupplemental.so', `
                'libSystem.IO.Compression.Native.so', `
                'libSystem.Native.so', `
                'libSystem.Security.Cryptography.Native.Android.so', `
                'libxamarin-app.so', `
                'Microsoft.Extensions.Configuration.Abstractions.dll.so', `
                'Microsoft.Extensions.Configuration.Binder.dll.so', `
                'Microsoft.Extensions.Configuration.dll.so', `
                'Microsoft.Extensions.DependencyInjection.Abstractions.dll.so', `
                'Microsoft.Extensions.DependencyInjection.dll.so', `
                'Microsoft.Extensions.Http.dll.so', `
                'Microsoft.Extensions.Logging.Abstractions.dll.so', `
                'Microsoft.Extensions.Logging.Configuration.dll.so', `
                'Microsoft.Extensions.Logging.dll.so', `
                'Microsoft.Extensions.Options.ConfigurationExtensions.dll.so', `
                'Microsoft.Extensions.Options.dll.so', `
                'Microsoft.Extensions.Primitives.dll.so', `
                'Microsoft.Maui.Controls.Compatibility.dll.so', `
                'Microsoft.Maui.Controls.dll.so', `
                'Microsoft.Maui.Controls.Xaml.dll.so', `
                'Microsoft.Maui.dll.so', `
                'Microsoft.Maui.Essentials.dll.so', `
                'Microsoft.Maui.Graphics.dll.so', `
                'Microsoft.Win32.Primitives.dll.so', `
                'Mono.Android.dll.so', `
                'Sentry.Android.AssemblyReader.dll.so', `
                'Sentry.Android.AssemblyReader.pdb', `
                'Sentry.Bindings.Android.dll.so', `
                'Sentry.Bindings.Android.pdb', `
                'Sentry.dll.so', `
                'Sentry.Extensions.Logging.dll.so', `
                'Sentry.Extensions.Logging.pdb', `
                'Sentry.Maui.dll.so', `
                'Sentry.Maui.pdb', `
                'Sentry.pdb', `
                'Sentry.Samples.Maui.dll.so', `
                'Sentry.Samples.Maui.pdb', `
                'System.Collections.Concurrent.dll.so', `
                'System.Collections.dll.so', `
                'System.Collections.Immutable.dll.so', `
                'System.Collections.NonGeneric.dll.so', `
                'System.Collections.Specialized.dll.so', `
                'System.ComponentModel.dll.so', `
                'System.ComponentModel.Primitives.dll.so', `
                'System.ComponentModel.TypeConverter.dll.so', `
                'System.Console.dll.so', `
                'System.Diagnostics.DiagnosticSource.dll.so', `
                'System.Diagnostics.StackTrace.dll.so', `
                'System.Diagnostics.TraceSource.dll.so', `
                'System.dll.so', `
                'System.IO.Compression.dll.so', `
                'System.IO.Compression.ZipFile.dll.so', `
                'System.IO.FileSystem.DriveInfo.dll.so', `
                'System.IO.MemoryMappedFiles.dll.so', `
                'System.Linq.dll.so', `
                'System.Linq.Expressions.dll.so', `
                'System.Memory.dll.so', `
                'System.Net.Http.dll.so', `
                'System.Net.NetworkInformation.dll.so', `
                'System.Net.Primitives.dll.so', `
                'System.Net.Requests.dll.so', `
                'System.Net.Security.dll.so', `
                'System.Net.WebProxy.dll.so', `
                'System.Numerics.Vectors.dll.so', `
                'System.ObjectModel.dll.so', `
                'System.Private.CoreLib.dll.so', `
                'System.Private.Uri.dll.so', `
                'System.Private.Xml.dll.so', `
                'System.Reflection.Metadata.dll.so', `
                'System.Reflection.Primitives.dll.so', `
                'System.Runtime.CompilerServices.Unsafe.dll.so', `
                'System.Runtime.dll.so', `
                'System.Runtime.InteropServices.dll.so', `
                'System.Runtime.InteropServices.RuntimeInformation.dll.so', `
                'System.Runtime.Serialization.Primitives.dll.so', `
                'System.Security.Cryptography.Algorithms.dll.so', `
                'System.Security.Cryptography.Primitives.dll.so', `
                'System.Text.Encodings.Web.dll.so', `
                'System.Text.Json.dll.so', `
                'System.Text.RegularExpressions.dll.so', `
                'System.Threading.dll.so', `
                'System.Threading.Thread.dll.so', `
                'System.Threading.ThreadPool.dll.so', `
                'System.Xml.ReaderWriter.dll.so', `
                'Xamarin.AndroidX.Activity.dll.so', `
                'Xamarin.AndroidX.AppCompat.AppCompatResources.dll.so', `
                'Xamarin.AndroidX.AppCompat.dll.so', `
                'Xamarin.AndroidX.CardView.dll.so', `
                'Xamarin.AndroidX.Collection.dll.so', `
                'Xamarin.AndroidX.CoordinatorLayout.dll.so', `
                'Xamarin.AndroidX.Core.dll.so', `
                'Xamarin.AndroidX.CursorAdapter.dll.so', `
                'Xamarin.AndroidX.CustomView.dll.so', `
                'Xamarin.AndroidX.DrawerLayout.dll.so', `
                'Xamarin.AndroidX.Fragment.dll.so', `
                'Xamarin.AndroidX.Lifecycle.Common.dll.so', `
                'Xamarin.AndroidX.Lifecycle.LiveData.Core.dll.so', `
                'Xamarin.AndroidX.Lifecycle.ViewModel.dll.so', `
                'Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll.so', `
                'Xamarin.AndroidX.Loader.dll.so', `
                'Xamarin.AndroidX.Navigation.Common.dll.so', `
                'Xamarin.AndroidX.Navigation.Fragment.dll.so', `
                'Xamarin.AndroidX.Navigation.Runtime.dll.so', `
                'Xamarin.AndroidX.Navigation.UI.dll.so', `
                'Xamarin.AndroidX.RecyclerView.dll.so', `
                'Xamarin.AndroidX.SavedState.dll.so', `
                'Xamarin.AndroidX.SwipeRefreshLayout.dll.so', `
                'Xamarin.AndroidX.ViewPager.dll.so', `
                'Xamarin.AndroidX.ViewPager2.dll.so', `
                'Xamarin.Google.Android.Material.dll.so', `
                'Xamarin.Kotlin.StdLib.dll.so', `
                'Xamarin.KotlinX.Coroutines.Core.Jvm.dll.so' `
        )
        @(Compare-Object -ReferenceObject $expected -DifferenceObject $actual -PassThru) | Should -Be @()
    }
}
