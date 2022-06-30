using System;
using Android.App;
using Android.OS;
using System.Collections.Generic;
using System.IO;
using Android.Util;
using Microsoft.DotNet.XHarness.DefaultAndroidEntryPoint.Xunit;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Sentry.Protocol.Tests.Context;
using Sentry.Tests;
using Sentry.Samples.Android.Kotlin;

namespace Sentry.Samples.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var path = Path.Combine(FilesDir.AbsolutePath, "result");

            var a = new DefaultAndroidEntryPoint(
                path,
                new Dictionary<string, string>());

            a.MinimumLogLevel = MinimumLogLevel.Debug;
            a.Tests = new[]
            {
                // this.GetType().Assembly
                typeof(AppTests).Assembly
            };

            a.TestsStarted += (sender, args) => Log.Info("Sentry.Android.Tests","Test started");
            a.TestsCompleted += (sender, args) => Log.Info("Sentry.Android.Tests","Test completed");
            try
            {
                a.RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Log.Info("Sentry.Android.Tests","Failed: " + e);
                throw;
            }

            Log.Info("Sentry.Android.Tests","RunAsync returned");
            // Set our view from the "main" layout resource
            // SetContentView(Resource.Layout.activity_main);
        }
    }
}
