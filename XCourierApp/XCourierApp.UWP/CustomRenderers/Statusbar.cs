//using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;
using XCourierApp;
using MyDemo.UWP.CustomRenderers;

[assembly: Dependency(typeof(Statusbar))]
namespace MyDemo.UWP.CustomRenderers
{
    public class Statusbar : IStatusBarPlatformSpecific
    {
        public Statusbar()
        {
            //TODO
        }

        public void SetStatusBarColor(Color color)
        {
            //TODO
            /*
            // The SetStatusBarcolor is new since API 21
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                var androidColor = color.AddLuminosity(-0.1).ToAndroid();
                //Just use the plugin
                CrossCurrentActivity.Current.Activity.Window.SetStatusBarColor(androidColor);
            }
            else
            {
                // Here you will just have to set your 
                // color in styles.xml file as shown below.
            }
            */
        }
    } // class
} // namespace