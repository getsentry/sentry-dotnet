using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Sentry.MauiTrimTest;

public class Program : MauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private static void Main(string[] args)
    {
        var app = new Program();
        app.Run(args);
    }

    public Program(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }
}
