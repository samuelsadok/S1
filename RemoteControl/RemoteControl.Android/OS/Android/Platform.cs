using System;
using System.Threading;
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
        public static PlatformType Type { get { return PlatformType.Android; } }

        /// <summary>
        /// Returns the current system theme color, the device color or a predefined color.
        /// You should normally use Application.ThemeColor instead.
        /// On Android there seems to be no way of determining a global theme color, hence this returns a constant. todo: adapt constant correctly.
        /// </summary>
        public static Color SystemThemeColor { get { return Color.Blue; } }

        /// <summary>
        /// Returns the main activity that represents this application.
        /// This is set by the framework before the Application class is instantiated and should not be changed afterwards.
        /// </summary>
        public static Activity Activity { get; set; }

        /// <summary>
        /// Executes a routine in the context of the main thread (in GUI apps this is the GUI thread) and blocks until the execution finishes.
        /// This can also be called from the main thread.
        /// </summary>
        public static void InvokeMainThread(Action action)
        {
            Activity.RunOnUiThread(action);
        }

        /// <summary>
        /// Evaluates a function in the context of the main thread (in GUI apps this is the GUI thread) and returns the result.
        /// This can also be called from the main thread.
        /// </summary>
        public static T EvaluateOnMainThread<T>(Func<T> function)
        {
            T result = default(T);
            InvokeMainThread(() => result = function());
            return result;
        }


        /// <summary>
        /// Displays a message box on top of the current window.
        /// This function should not be used in a production scenario, as it's very intrusive to the user.
        /// </summary>
        public static void MsgBox(string message, string title)
        {
            ManualResetEvent done = new ManualResetEvent(false);
            InvokeMainThread(() => {
                new AlertDialog.Builder(Activity)
                    .SetMessage(message)
                    .SetOnDismissListener(new PlatformUtilities.DialogListener((dialog) => done.Set()))
                    .Show();
            });
            done.WaitOne();
        }


        /// <summary>
        /// Opens the specified URL in the standard webbrowser
        /// </summary>
        public static void OpenWebPage(string url)
        {
            Android.Net.Uri webpage = Android.Net.Uri.Parse(url);
            Intent intent = new Intent(Intent.ActionView, webpage);
            if (intent.ResolveActivity(Activity.PackageManager) != null) {
                Activity.StartActivity(intent);
            }
        }

        public static LogContext DefaultLog { get { return debugLog; } }
        private static LogContext debugLog = new LogContext((c, m, t) => { Console.WriteLine(c + ": " + m); }, "root");
    }
}