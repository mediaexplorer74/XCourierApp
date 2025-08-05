//using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;
using XCourierApp;
using MyDemo.UWP.CustomRenderers;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;

[assembly: Dependency(typeof(Statusbar))]
namespace MyDemo.UWP.CustomRenderers
{
    public class Statusbar
    {
        public Statusbar()
        {
            //TODO
        }

        public void SetStatusBarColor(Windows.UI.Color color)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.BackgroundColor = color;
                //Just use the plugin
                //CrossCurrentActivity.Current.Activity.Window.SetStatusBarColor(androidColor);
            }
            else
            {
                // Handle older versions of Windows
            }
        }
    }
}