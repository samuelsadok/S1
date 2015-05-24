using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using AppInstall.UI;
using AppInstall.Framework;
using AppInstall.Graphics;

namespace AppInstall.OS
{

    /// <summary>
    /// Represents platform specific or shared properties, instances, methodes and events
    /// </summary>
    public static class Platform
    {
        /// <summary>
        /// Returns the current platform.
        /// </summary>
        public static PlatformType Type { get { return PlatformType.iOS; } }

        /// <summary>
        /// Returns applications root path
        /// </summary>
        public static string ApplicationPath { get { return "."; } }

        /// <summary>
        /// Returns the path of the applications assets directory
        /// </summary>
        public static string AssetsPath { get { return ApplicationPath + "/Assets"; } }

        /// <summary>
        /// Returns the root view controller.
        /// </summary>
        //public static IViewController RootViewController { get; set; }

        /// <summary>
        /// Returns size of the screen (on mobile devices) or the window (on desktop devices) in which the app is displayed.
        /// </summary>
        public static Vector2D<float> ScreenSize { get { return UIScreen.MainScreen.Bounds.Size.ToVector2D(); } }

        /// <summary>
        /// Returns the height of the status bar
        /// </summary>
        public static float StatusBarHeight { get { return (UIApplication.SharedApplication.StatusBarHidden ? 0 : JustifyOrientation(UIApplication.SharedApplication.StatusBarOrientation, UIApplication.SharedApplication.StatusBarFrame.ToVector4D()).V4); } }

        /// <summary>
        /// Returns the height of the status bar
        /// </summary>
        //public static float TopLayoutGuide { get { return ((UIViewController)RootViewController).TopLayoutGuide.Length; } }

        /// <summary>
        /// Returns the current system theme color, the device color or a predefined color.
        /// You should normally use Application.ThemeColor instead.
        /// </summary>
        public static Color SystemThemeColor { get { return Color.Blue; } }

        /// <summary>
        /// Executes a routine in the context of the main thread (in GUI apps this is the GUI thread). This should also work in the main thread.
        /// </summary>
        /// <param name="block">Specifies if the routine should block until the operation was executed on the main thread</param>
        public static void InvokeMainThread(Action action, bool block = false)
        {
            ManualResetEvent done = new ManualResetEvent(false);
            if (NSThread.IsMain) {
                //Console.WriteLine("invoke from main");
                action();
            } else {
                //Console.WriteLine("invoke from other thread");
                //NSOperationQueue.MainQueue.AddOperation(delegate { action(); });
                Exception ex = null;
                NSOperationQueue.MainQueue.InvokeOnMainThread(delegate {
                    try {
                        action();
                    } catch (Exception exc) {
                        ex = exc;
                    }
                    done.Set();
                });
                done.WaitOne();
                if (ex != null) throw ex;
            }
            //Console.WriteLine("invoke exit");
        }

        public static T EvaluateOnMainThread<T>(Func<T> function)
        {
            T result = default(T);
            InvokeMainThread(() => result = function(), true);
            return result;
        }

        /// <summary>
        /// Swaps X and Y and Width and Height of a rectangle if the orientation is landscape
        /// </summary>
        public static Vector4D<float> JustifyOrientation(UIInterfaceOrientation orientation, Vector4D<float> rectangle)
        {
            if ((orientation == UIInterfaceOrientation.Portrait) || (orientation == UIInterfaceOrientation.PortraitUpsideDown)) return rectangle;
            return new Vector4D<float>(rectangle.V2, rectangle.V1, rectangle.V4, rectangle.V3);
        }

        public static void MsgBox(string message, string title)
        {
            ManualResetEvent done = new ManualResetEvent(false);
            UIAlertView v = null;
            try {
                InvokeMainThread(() => {
                    v = new UIAlertView(title, message, null, "OK");
                    v.Show();
                    v.Dismissed += (o, e) => done.Set();
                });
                done.WaitOne();
            } finally {
                if (v != null) v.Dispose();
            }
        }

        public static void DumpDir(string directory)
        {
            try {
                DefaultLog.Log(directory + ":");
                foreach (var dir in global::System.IO.Directory.EnumerateDirectories(directory)) {
                    DefaultLog.Log("dir " + dir);
                }
                foreach (var file in global::System.IO.Directory.EnumerateFiles(directory)) {
                    DefaultLog.Log("file " + file);
                }
            } catch (Exception ex) {
                DefaultLog.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Opens the specified URL in the standard webbrowser
        /// </summary>
        public static void OpenWWW(string URL)
        {
            UIApplication.SharedApplication.OpenUrl(NSUrl.FromString(URL));
        }

        public static LogContext DefaultLog { get { return debugLog; } }
        private static LogContext debugLog = new LogContext((c, m, t) => { Console.WriteLine(c + ": " + m); }, "root");
    }
}