using AppInstall.Framework;
using AppInstall.OS;

namespace AppInstall.UI
{
    public class ContainerView : View<Android.Widget.RelativeLayout>
    {

        protected ContainerView()
            : base(new Android.Widget.RelativeLayout(Platform.Activity))
        {
            //nativeView.SetGravity(Android.Views.GravityFlags.Left | Android.Views.GravityFlags.Top);
        }

        /// <summary>
        /// Adds the specified subview to this view in front of all other subviews.
        /// Prior to adding the subview, it is removed from its previous parent.
        /// If the subview is already contained by this view, it is only brought to the front.
        /// </summary>
        /// <param name="view">the subview to be added</param>
        /// <param name="toFront">if false, the view is added behind all other views</param>
        protected void AddSubview(View view, bool toFront = true)
        {
            if (view.NativeView.Parent != null)
                ((Android.Views.ViewGroup)view.NativeView.Parent).RemoveView(view.NativeView);

            // var layoutParams = new Android.Widget.RelativeLayout.LayoutParams(Android.Widget.RelativeLayout.LayoutParams.WrapContent, Android.Widget.RelativeLayout.LayoutParams.WrapContent);
            // layoutParams.AddRule(LayoutRules.AlignParentLeft);
            // layoutParams.AddRule(LayoutRules.AlignParentTop);
            // view.NativeView.LayoutParameters = layoutParams;

            Application.UILog.Log("alt container parent: " + view.NativeView.Parent);

            if (toFront)
                nativeView.AddView(view.NativeView);
            else
                nativeView.AddView(view.NativeView, 0);
        }

        /// <summary>
        /// Removes a subview.
        /// No action is taken if the specified subview is not a subview of this view. todo: verify
        /// </summary>
        protected void RemoveSubview(View view)
        {
            if (view.NativeView.Parent == nativeView)
                nativeView.RemoveView(view.NativeView);
        }

        /// <summary>
        /// Replaces a subview of this view by a new view.
        /// </summary>
        /// <param name="oldView">The view to be removed. Can be null.</param>
        /// <param name="newView">The view to be added.</param>
        /// <returns>The view that was added</returns>
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
            return subview.location;
        }

        /// <summary>
        /// Moves the specified subview to the specified location.
        /// </summary>
        protected void SetLocation(View subview, Vector2D<float> location)
        {
            subview.location = location;
        }

        /// <summary>
        /// Brings the specified view to the front.
        /// </summary>
        public void BringToFront(View view)
        {
            nativeView.RemoveView(view.NativeView);
            nativeView.AddView(view.NativeView);
        }

        /// <summary>
        /// Sends the specified view to the back.
        /// </summary>
        public void SendToBack(View view)
        {
            nativeView.RemoveView(view.NativeView);
            nativeView.AddView(view.NativeView, 0);
        }
    }
}