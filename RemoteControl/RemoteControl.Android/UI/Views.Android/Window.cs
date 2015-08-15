using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using AppInstall.Framework;
using AppInstall.Graphics;
using AppInstall.OS;

namespace AppInstall.UI
{
    /// <summary>
    /// Represents a container that can show a view or view controller.
    /// This does currently not map to an Android window, instead, when calling Show(), the view is displayed on the application's main activity.
    /// </summary>
    public class Window
    {
        View view;
        ViewController viewController;

        /// <summary>
        /// Never triggerd on iOS - only provided for compatibility
        /// </summary>
        public event Action Closed;


        public Window(ViewController viewController, Color themeColor)
        {
            if (viewController == null) throw new ArgumentNullException(nameof(view));
            if (themeColor == null) throw new ArgumentNullException(nameof(themeColor));

            view = new LayerLayout();
            this.viewController = viewController;
            //view.NativeView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent); ;
        }


        public Window(ViewController view)
            : this(view, Application.ThemeColor)
        {
        }


        public void Show()
        {
            var platformView = new PlatformViewWrapper(view);
            var actualView = viewController.ConstructView();
            Application.UILog.Log("alt window parent: " + platformView.NativeView.Parent);
            //Platform.Activity.SetContentView(platformView.NativeView);
            ((LayerLayout)view).Insert(actualView, true);

            var d = Platform.Activity.Resources.DisplayMetrics.Density;
            var testView = new Android.Widget.RelativeLayout(Platform.Activity);

            var testBtn = new Android.Widget.Button(Platform.Activity) { Text = "hello" };
            var p = new Android.Widget.RelativeLayout.LayoutParams((int)(d * 400f), (int)(d * 100f));
            testView.AddView(testBtn, p);
            p.LeftMargin = (int)(d * 200f);
            p.TopMargin = (int)(d * 100f);

            new System.Threading.Tasks.Task(() => {
                while (true) {
                    try {
                        System.Threading.Tasks.Task.Delay(3000).Wait();
                        Platform.InvokeMainThread(() => {
                            p.Width = (int)(d * 300f);
                            p.Height = (int)(d * 80f);
                            p.LeftMargin = (int)(d * 100f);
                            p.TopMargin = (int)(d * 0f);
                            testBtn.RequestLayout();
                        });

                        System.Threading.Tasks.Task.Delay(3000).Wait();
                        Platform.InvokeMainThread(() => {
                            p.Width = (int)(d * 400f);
                            p.Height = (int)(d * 100f);
                            p.LeftMargin = (int)(d * 200f);
                            p.TopMargin = (int)(d * 100f);
                            testBtn.RequestLayout();
                        });
                    } catch (Exception ex) {
                        Application.UILog.Log("error: " + ex);
                    }
                }
            }).Start();

            Platform.Activity.SetContentView(testView);
        }

        public void DumpLayout(LogContext logContext)
        {
            var str = new StringBuilder(256);
            view.DumpLayout(str, "");
            logContext.Log(str.ToString());
        }
    }
}