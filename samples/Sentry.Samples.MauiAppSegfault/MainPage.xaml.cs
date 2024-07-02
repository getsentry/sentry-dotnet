using System;

namespace MauiAppSegfault
{
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_OnClicked(object sender, EventArgs e)
        {
#if ANDROID
            Android.Util.Log.Info("MauiAppSegFault", "Button_OnClicked");
#endif
            try
            {
                var s = default(string);
                var c = s.Length;
            }
            catch (Exception ex)
            {
#if ANDROID
                Android.Util.Log.Info("MauiAppSegFault", $"Exception catched !!! {Environment.NewLine}{ex}");
#endif
            }
        }
    }
}
