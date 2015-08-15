using System;
using System.Linq;
using System.Collections.Generic;
using AppInstall.Framework;
using AppInstall.Graphics;
using AppInstall.OS;

namespace AppInstall.UI
{

    /// <summary>
    /// Provides operators for casting between platform specific and platform independent datatypes
    /// </summary>
    public static class Abstraction
    {

        public static Vector2D<float> ToVector2D(this Android.Graphics.Point point)
        {
            return new Vector2D<float>(point.X, point.Y);
        }
        public static Vector2D<float> ToVector2D(this Android.Graphics.PointF point)
        {
            return new Vector2D<float>(point.X, point.Y);
        }
        public static Vector4D<float> ToVector4D(this Android.Graphics.Rect rectangle)
        {
            return new Vector4D<float>(rectangle.Left, rectangle.Top, rectangle.Width(), rectangle.Height());
        }
        public static Vector4D<float> ToVector4D(this Android.Graphics.RectF rectangle)
        {
            return new Vector4D<float>(rectangle.Left, rectangle.Top, rectangle.Width(), rectangle.Height());
        }
        public static Android.Graphics.Point ToPoint(this Vector2D<float> vector)
        {
            return new Android.Graphics.Point((int)vector.X, (int)vector.Y);
        }
        public static Android.Graphics.PointF ToPointF(this Vector2D<float> vector)
        {
            return new Android.Graphics.PointF(vector.X, vector.Y);
        }
        public static Android.Graphics.Rect ToRect(this Vector4D<float> vector)
        {
            return new Android.Graphics.Rect((int)vector.V1, (int)vector.V2, (int)vector.V3, (int)vector.V4);
        }
        public static Android.Graphics.RectF ToRectF(this Vector4D<float> vector)
        {
            return new Android.Graphics.RectF(vector.V1, vector.V2, vector.V3, vector.V4);
        }

        public static Color ToColor(this Android.Graphics.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }
        public static Android.Graphics.Color ToUIColor(this Color color)
        {
            return new Android.Graphics.Color((byte)(color.R * 255f), (byte)(color.G * 255f), (byte)(color.B * 255f), (byte)(color.A * 255f));
        }
    }


    public partial class Animation
    {
        private static Animation currentAnimation = null;
        private List<Android.Animation.ValueAnimator> changes;

        public static void AnimateOpacity(View view, float from, float to)
        {
            if (currentAnimation == null) {
                view.NativeView.Alpha = to;
                return;
            }

            view.NativeView.HasTransientState = true;

            Android.Animation.ValueAnimator animator = Android.Animation.ValueAnimator.OfFloat(from, to);
            animator.Update += (o, e) => view.NativeView.Alpha = (float)e.Animation.AnimatedValue;
            animator.AnimationEnd += (o, e) => {
                view.NativeView.Alpha = to;
                view.NativeView.HasTransientState = false;
            };
            currentAnimation.changes.Add(animator);
        }

        private void PlatformExecute(int duration)
        {
            changes = new List<Android.Animation.ValueAnimator>();

            currentAnimation = this;
            InvokeAnimatedAction();
            currentAnimation = null;

            if (changes.Any())
                changes.Last().AnimationEnd += (o, e) => InvokeEndAction();
            else
                InvokeEndAction();

            foreach (var change in changes) {
                change.SetDuration(duration);
                change.Start();
            }
        }
    }

    public class Dialog
    {
        Window parent;
        ViewController view;
        Android.App.Dialog dialog;

        public Dialog(Window parent, ViewController view)
        {
            this.parent = parent;
            this.view = view;

            
        }

        /// <summary>
        /// On large screens: displays a view controller in a new pop-over window.
        /// On small screens: displays a view on top of the current window.
        /// This can be called in a non-UI thread.
        /// </summary>
        public void Show()
        {
            Platform.InvokeMainThread(() => {
                dialog = new Android.App.Dialog(Platform.Activity, Android.Resource.Style.ThemeDeviceDefaultNoActionBarFullscreen);
                dialog.SetContentView(view.ConstructView().NativeView);
                dialog.Show();
            });
        }

        /// <summary>
        /// Closes the dialog.
        /// This can be called in a non-UI thread.
        /// </summary>
        public void Close()
        {
            Platform.InvokeMainThread(() => {
                dialog.Dismiss();
                dialog = null;
            });
        }
    }
}