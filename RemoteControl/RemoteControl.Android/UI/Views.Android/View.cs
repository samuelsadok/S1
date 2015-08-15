using System;
using AppInstall.Framework;
using AppInstall.OS;

namespace AppInstall.UI
{
    public abstract class View<T> : View where T : Android.Views.View
    {
        protected new T nativeView;

        /// <summary>
        /// Creates a new view based on the provided platform view.
        /// By default, built-in functions are used to determine the minimum size and update the layout.
        /// This behavior can be customized by overriding the GetContentSize and UpdateContentLayout methods.
        /// </summary>
        protected View(T view)
            : base(view)
        {
            nativeView = view;
        }

        protected override Vector2D<float> GetContentSize(Vector2D<float> maxSize)
        {
            nativeView.Measure(
                Android.Views.View.MeasureSpec.MakeMeasureSpec((int)maxSize.X, float.IsInfinity(maxSize.X) ? Android.Views.MeasureSpecMode.Unspecified : Android.Views.MeasureSpecMode.AtMost),
                Android.Views.View.MeasureSpec.MakeMeasureSpec((int)maxSize.Y, float.IsInfinity(maxSize.Y) ? Android.Views.MeasureSpecMode.Unspecified : Android.Views.MeasureSpecMode.AtMost)
                );
            return new Vector2D<float>(nativeView.MeasuredWidth, nativeView.MeasuredHeight);
        }

        protected override void UpdateContentLayout()
        {

        }
    }


    public abstract class View
    {

        // todo: improve efficiency of layout updates by altering the design (separate measure and layout pass, layout only if neccessary)

        protected Android.Views.View nativeView;

        /// <summary>
        /// Returns the native view object associated with this wrapper.
        /// </summary>
        public Android.Views.View NativeView { get { return nativeView; } }

        public Margin Padding { get; set; } // left - right - top - bottom
        public Margin Margin { get; set; } // left - right - top - bottom
        //public Vector2D<float> Location { get; set; } // X - Y
        public Vector2D<float> Size { get; set; } // width - height
        public float Opacity { get { return nativeView.Alpha; } set { Animation.AnimateOpacity(this, nativeView.Alpha, value); } }
        public bool Shadow { get; set; }
        public bool Autosize { get; set; }



        /// <summary>
        /// This is only for use by the ContainerView, since a location is only meaningful in the context of a parent.
        /// </summary>
        public Vector2D<float> location = new Vector2D<float>(0, 0);


        public event Action<View> WillUpdateLayout;


        /// <summary>
        /// Creates a new view from a platform specific view object.
        /// If custom padding is requested, the UpdateLayout method aligns the contents
        /// frame with the specified Location and Size and leaves padding to the overridden UpdateContentLayout method.
        /// Else, the the content size is set to the specified size minus the padding.
        /// </summary>
        /// <param name="nativeView"></param>
        /// <param name="builinPadding"></param>
        protected View(Android.Views.View nativeView)
        {
            if (nativeView == null) throw new ArgumentNullException(nameof(nativeView));
            this.nativeView = nativeView;
            nativeView.Enabled = true;
            nativeView.Visibility = Android.Views.ViewStates.Visible;
            Padding = new Margin();
            Margin = new Margin();
            Size = new Vector2D<float>(nativeView.Width, nativeView.Height);


            if (Shadow) { // todo: this method may not be optimal
                nativeView.Background = AppInstall.OS.Platform.Activity.GetDrawable(Android.Resource.Drawable.DialogFrame);
            }
        }

        /// <summary>
        /// Returns true if there is no way to see through the view
        /// </summary>
        public virtual bool IsOpaque()
        {
            return nativeView.IsOpaque;
        }

        /// <summary>
        /// This method must be overridden by an inheriting class and should return the minimum size of the content
        /// given the specified max size and respecting the childrens's margins.
        /// todo: consider a separate measure and layout pass: store the content size instead of returning it
        /// </summary>
        protected abstract Vector2D<float> GetContentSize(Vector2D<float> maxSize);

        /// <summary>
        /// Returns the minimum size of the view so that it would enclose the content, respecting the own padding and the children's margins.
        /// </summary>
        public Vector2D<float> GetMinSize(Vector2D<float> maxSize)
        {
            return GetMinSize(maxSize, Padding);
        }

        /// <summary>
        /// Returns the minimum size of the view so that it would enclose the content, respecting the specified padding and the children's margins.
        /// </summary>
        public Vector2D<float> GetMinSize(Vector2D<float> maxSize, Margin padding)
        {
            if (maxSize == null) throw new ArgumentNullException($"{maxSize}");
            if (padding == null) throw new ArgumentNullException($"{padding}");

            var paddingOverhead = new Vector2D<float>(padding.Left + padding.Right, padding.Top + padding.Bottom);
            var contentSize = GetContentSize(maxSize - paddingOverhead);

            if (contentSize == null) throw new MethodAccessException("the derived class of type " + this.GetType() + " returned a content size of null.");
            return contentSize + paddingOverhead;
        }

        /// <summary>
        /// This method must be overridden by an inheriting class and should update the layout of the view content.
        /// </summary>
        protected abstract void UpdateContentLayout();

        /// <summary>
        /// Arranges the subviews to fit the new layout
        /// </summary>
        public void UpdateLayout()
        {
            if (Autosize)
                Size = GetMinSize(new Vector2D<float>(float.MaxValue, float.MaxValue));

            WillUpdateLayout.SafeInvoke(this);

            nativeView.SetPadding((int)Padding.Left, (int)Padding.Top, (int)Padding.Right, (int)Padding.Bottom);
            nativeView.Layout((int)location.X, (int)location.Y, (int)Size.X, (int)Size.Y);

            UpdateContentLayout();
        }


        /// <summary>
        /// Generates a dump that describes the layout of this view and it's children.
        /// </summary>
        /// <param name="dump">A string builder to which the dump is appended</param>
        /// <param name="indent">The indentation prefix to be used for each dump line</param>
        /// <param name="tag">A tag that describes the role of this view</param>
        public virtual void DumpLayout(System.Text.StringBuilder dump, string indent, string tag = null)
        {
            dump.AppendLine(indent + GetType() + (tag == null ? "" : " (" + tag + ")") + ", size: " + Size + ", platform: " + nativeView.Width + "x" + nativeView.Height);
        }
        protected const string DUMP_INDENT_STEP = "|   ";
    }



    
    public class PlatformViewWrapper : View<Android.Widget.RelativeLayout>
    {
        private View innerView;
        private Func<View, bool> layoutActions;

        /// <summary>
        /// Creates a platform wrapper for the specified view.
        /// If the platform resizes the wrapper view, the inner view is resized accordingly.
        /// If the code resizes or moves the inner view, the wrapper view is resized and moved accordingly.
        /// Note that the location of the inner view will always be reset to zero and only the wrapper view is actually moved.
        /// Do not directly modify properties (especially Location and Size) on this wrapper instance.
        /// Note: the location property is currently not properly transferred from the inner view.
        /// </summary>
        /// <param name="innerView">The view that should be encapsulated.</param>
        /// <param name="layoutActions">This handler can be used to enforce layout constraints. It will be invoked prior to every layout update and should return true if it altered the Size property of the inner view.</param>
        public PlatformViewWrapper(View innerView, Func<View, bool> layoutActions = null)
            : base(new Android.Widget.RelativeLayout(Platform.Activity))
        {
            this.layoutActions = layoutActions;
            this.innerView = innerView;

            nativeView.LayoutParameters = new Android.Views.ViewGroup.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent);
            Application.UILog.Log("alt platformview parent: " + innerView.NativeView.Parent);
            nativeView.AddView(innerView.NativeView);

            Size = innerView.GetMinSize(new Vector2D<float>(float.MaxValue, float.MaxValue));
            UpdateLayout();

            nativeView.LayoutChange += (o, e) => TransferLayout(true);
            innerView.WillUpdateLayout += (o) => TransferLayout(false);
        }

        private void TransferLayout(bool fromPlatform)
        {
            Application.UILog.Log("layout of " + innerView + " from " + (fromPlatform ? "platform" : "code"));

            var oldPadding = innerView.Padding.Copy();

            if (layoutActions != null)
                fromPlatform = !layoutActions(innerView) && fromPlatform;

            // only sync properties if they became inconsistent to prevent infinite recursion.
            var zeroVector = new Vector2D<float>(0, 0);
            var platformSize = new Vector2D<float>(nativeView.Width, nativeView.Height);

            Application.UILog.Log("inner size " + innerView.Size + " and platform frame " + platformSize);
            if (platformSize != innerView.Size || Size != innerView.Size || oldPadding != innerView.Padding) {
                Application.UILog.Log("size of " + innerView + " changed from " + (fromPlatform ? "platform" : "code"));

                if (fromPlatform)
                    innerView.Size = Size = platformSize;
                else
                    Size = innerView.Size;

                UpdateLayout();
                innerView.UpdateLayout();
                //content.LayoutSubviews();
            }
        }

        protected override Vector2D<float> GetContentSize(Vector2D<float> maxSize)
        {
            return innerView.GetMinSize(maxSize);
        }

        protected override void UpdateContentLayout()
        {
        }
    }

    
}