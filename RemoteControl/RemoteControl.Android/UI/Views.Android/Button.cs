using System;
using AppInstall.Framework;
using AppInstall.Graphics;
using AppInstall.OS;

namespace AppInstall.UI
{
    public class Button : View<Android.Widget.Button>
    {
        public event Action<Button> Triggered;

        public string Text { get { return nativeView.Text; } set { nativeView.Text = value; } }
        public float FontSize { get { return nativeView.TextSize; } set { nativeView.TextSize = value; } }
        public Color TextColor
        {
            get
            {
                var argb = nativeView.CurrentTextColor;
                return new Color(argb >> 16 & 0xFF, argb >> 8 & 0xFF, argb >> 0 & 0xFF, argb >> 24 & 0xFF);
            }
            set { nativeView.SetTextColor(value.ToUIColor()); }
        }

        public Button()
            : base(new Android.Widget.Button(Platform.Activity)) // todo: implement padding
        {
            nativeView.Click += (o, e) => Triggered.SafeInvoke(this);
        }

        protected override Vector2D<float> GetContentSize(Vector2D<float> maxSize)
        {
            return PlatformUtilities.MeasureStringSize(Text, nativeView.Paint, maxSize);
        }
    }
}