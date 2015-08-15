using System.Linq;
using Android.Content;
using System;

namespace AppInstall.Framework
{
    public static class PlatformUtilities
    {

        /// <summary>
        /// Returns the size that the specified string would take up if drawn within the specified bounds.
        /// </summary>
        public static Vector2D<float> MeasureStringSize(string str, Android.Graphics.Paint font, Vector2D<float> maxSize)
        {
            // else: set typeface, fontsize
            var size = new Android.Graphics.Rect();
            font.GetTextBounds(str, 0, str.Length, size);
            return new Vector2D<float>(size.Width(), size.Height());
        }

        /// <summary>
        /// Returns the largest size that the specified strings would take up if drawn within the specified bounds.
        /// Both X and Y dimensions are chosen separately from the largest value.
        /// </summary>
        /// <param name="strings">The strings that should be measured. Null or whitespace strings are ignored.</param>
        public static Vector2D<float> MeasureStringSize(Android.Graphics.Paint font, Vector2D<float> maxSize, params string[] strings)
        {
            return (from str in strings where !string.IsNullOrWhiteSpace(str) select MeasureStringSize(str, font, maxSize)).Max();
        }

        public class DialogListener : Java.Lang.Object, IDialogInterfaceOnClickListener, IDialogInterfaceOnCancelListener, IDialogInterfaceOnDismissListener
        {
            private readonly Action<IDialogInterface> callback;
            public DialogListener(Action<IDialogInterface> callback)
            {
                if (callback == null)
                    throw new ArgumentNullException($"{callback}");
                this.callback = callback;
            }

            public void OnClick(IDialogInterface dialog, int which)
            {
                callback(dialog);
            }

            public void OnCancel(IDialogInterface dialog)
            {
                callback(dialog);
            }

            public void OnDismiss(IDialogInterface dialog)
            {
                callback(dialog);
            }
        }
    }
}