using Android.App;
using Android.OS;
using AppInstall.Framework;

namespace AppInstall.OS
{
    [Activity(Label = "AppInstall App", MainLauncher = true, Icon = "@drawable/icon")]
    public class Main : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Platform.Activity = this;

            ApplicationControl.Start(new string[0]);
        }

        
    }
}