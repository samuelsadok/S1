using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace AppInstall.OS
{

    public enum PlatformType {

        /// <summary>
        /// The platform could not be determined (why on earth would this ever happen?)
        /// </summary>
        Unknown,

        /// <summary>
        /// The application is running as a classic Windows desktop app.
        /// Console applications and services on Windows are also considered classic Windows apps.
        /// </summary>
        WindowsDesktop,

        /// <summary>
        /// The application is running as a Windows metro app (modern UI).
        /// </summary>
        WindowsMetro,

        /// <summary>
        /// The application is running on Linux (other than Android)
        /// </summary>
        Linux,

        /// <summary>
        /// The application is running on Mac OSX.
        /// </summary>
        OSX,

        /// <summary>
        /// The application is running on iOS.
        /// </summary>
        iOS,

        /// <summary>
        /// The application is running on Android.
        /// </summary>
        Android

    }

}