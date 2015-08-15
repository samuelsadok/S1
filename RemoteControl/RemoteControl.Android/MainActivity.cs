using System;
using System.Threading.Tasks;
using AppInstall.OS;
using AppInstall.Framework;
using AppInstall.Graphics;
using AppInstall.UI;

namespace AppInstall
{

    class Application
    {
        static LogContext uiLog = Platform.DefaultLog.SubContext("UI");
        public static LogContext UILog { get { return uiLog; } }

        public static string ApplicationName { get { return "Remote Control"; } }
        public static Color ThemeColor { get { return Platform.SystemThemeColor; } }


        /// <summary>
        /// This routine should only be used to set up the initial GUI
        /// </summary>
        public Application(string[] args)
        {
        }


        /// <summary>
        /// Main thread
        /// </summary>
        public void Main()
        {
            Platform.InvokeMainThread(() => {


                // Set our view from the "main" layout resource


                /*
                var lLayout = new LinearLayout(Platform.Activity);
                lLayout.Orientation = Orientation.Vertical;
                lLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);


                var tView = new EditText(Platform.Activity);
                tView.SetTextIsSelectable(true);
                tView.SetText("Hello 3, This is a view created programmatically! ", TextView.BufferType.Editable);
                tView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                lLayout.AddView(tView);


                //Platform.Activity.SetContentView(lLayout);
                */




                var mainView = new CustomViewController<int>() {
                    ViewConstructor = () => {
                        var b = new Button();

                        return b;
                    }
                };


                Platform.DefaultLog.Log("creating window...");
                var win = new Window(mainView);
                win.Show();
                win.Closed += () => ApplicationControl.Shutdown();

                new Task(() => {
                    while (!ApplicationControl.ShutdownToken.WaitHandle.WaitOne(10000)) {
                        Platform.DefaultLog.Log("UI dump:");
                        win.DumpLayout(Platform.DefaultLog);
                    }
                }).Start();
                

            });
        }


        
    }


}

