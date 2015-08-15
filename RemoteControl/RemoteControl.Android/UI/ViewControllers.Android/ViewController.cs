using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppInstall.Framework;
using AppInstall.OS;

namespace AppInstall.UI
{

    /// <summary>
    /// A view controller on iOS may display its content in different modes depending on the situation in which it is shown.
    /// </summary>
    public abstract partial class ViewController
    {

        public abstract View ConstructView();
    }
}