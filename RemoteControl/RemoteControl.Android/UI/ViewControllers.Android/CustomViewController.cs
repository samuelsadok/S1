using System;
using AppInstall.Framework;

namespace AppInstall.UI
{
    class CustomViewController<T> : DataViewController<DataSource<T>>
    {
        public Func<View> ViewConstructor { get; set; }

        public override View ConstructView()
        {
            if (ViewConstructor == null)
                throw new InvalidOperationException($"{ViewConstructor} must not be {null}");
            return ViewConstructor();
        }

    }
}