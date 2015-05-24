
//#define TEST
//#define BROKEN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppInstall.Graphics;

namespace GDM
{
    public static class CustomConstants
    {

        public const string WEB_SERVER = "www.gdm-bauservice.ch"; // main webserver
        public const string HOMEPAGE = "http://" + WEB_SERVER + "/"; // main webserver
#if TEST
        public const string CALLHOME_SERVER = "10.211.55.3"; // callhome server for end user applications
#elif BROKEN
        public const string CALLHOME_SERVER = "178.196.90.214"; // callhome server for end user applications
#else
        public const string CALLHOME_SERVER = "innovation-labs.appinstall.ch"; // callhome server for end user applications
#endif
        public const string DB_SERVER = CALLHOME_SERVER;
        public const int DB_SERVER_PORT = 1330;
     
        public static Color ThemeColor = new Color(0x0E1655FF).AdjustBrightness(0.5f); // color taken from GDM logo on website
    }
}
