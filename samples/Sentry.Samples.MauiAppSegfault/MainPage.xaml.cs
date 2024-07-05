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
            try
            {
                var s = default(string);
                var c = s.Length;
            }
            catch
            {
                // ignored
            }
        }
    }
}
