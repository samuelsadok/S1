using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using AppInstall.Framework;

namespace AppInstall.UI
{
    public class ContainerView : View<UIView>
    {
        protected ContainerView()
            : base(true)
        {
        }

        //protected ContainerView(bool builtinPadding)
        //    : base(builtinPadding)
        //{
        //}

        protected void AddSubview(View view, bool toFront = true)
        {
            if (!nativeView.Subviews.Contains(view.NativeView))
                nativeView.AddSubview(view.NativeView);

            if (toFront)
                nativeView.BringSubviewToFront(view.NativeView);
            else
                nativeView.SendSubviewToBack(view.NativeView);
        }

        protected void RemoveSubview(View view)
        {
            if (nativeView.Subviews.Contains(view.NativeView))
                view.NativeView.RemoveFromSuperview();
        }

        /// <summary>
        /// Replaces a subview of this view by a new view
        /// </summary>
        /// <param name="oldView">the view to be removed</param>
        /// <param name="newView">the view to be added</param>
        /// <returns>the view that was added</returns>
        protected View ReplaceSubview(View oldView, View newView)
        {
            if (oldView != null) RemoveSubview(oldView);
            AddSubview(newView);
            return newView;
        }

        /// <summary>
        /// Returns the current location of the specified subview.
        /// </summary>
        protected Vector2D<float> GetLocation(View subview)
        {
            var location = subview.NativeView.Frame.Location.ToVector2D();
            if (subview.BuiltinPadding) return location;
            return new Vector2D<float>(location.X - subview.Padding.Left, location.Y - subview.Padding.Top);
        }
        /// <summary>
        /// Moves the specified subview to the specified location.
        /// </summary>
        protected void SetLocation(View subview, Vector2D<float> location)
        {
            if (!subview.BuiltinPadding) location += new Vector2D<float>(subview.Padding.Left, subview.Padding.Top);
            subview.NativeView.Frame = new System.Drawing.RectangleF(location.ToPoint(), subview.NativeView.Frame.Size);
        }

        /// <summary>
        /// Brings the specified view to the front.
        /// </summary>
        public void BringToFront(View view)
        {
            nativeView.BringSubviewToFront(view.NativeView);
        }

        /// <summary>
        /// Sends the specified view to the back.
        /// </summary>
        public void SendToBack(View view)
        {
            nativeView.SendSubviewToBack(view.NativeView);
        }
    }
}
