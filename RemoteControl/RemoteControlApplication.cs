using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using AppInstall.Framework;
using AppInstall.Framework.MemoryModels;
using AppInstall.OS;
using AppInstall.UI;
using AppInstall.Hardware;
using AppInstall.Organization;
using AppInstall.Graphics;
using AppInstall.Installer;


namespace AppInstall
{

    /// <summary>
    /// Platform independent implementation of the Remote Control application
    /// </summary>
    class Application
    {
        static LogContext uiLog = Platform.DefaultLog.SubContext("UI");
        public static LogContext UILog { get { return uiLog; } }

        public static string ApplicationName { get { return "Remote Control"; } }
        public static Color ThemeColor { get { return GlobalConstants.UIColor; } }


        Window mainWindow;
        LayerLayout topLayer = new LayerLayout();


        private ErrorView errView = new ErrorView() {
            Message = "This is some random text to test various aspects of the implementation of some functionality"
        };
        //private BluetoothDeviceListView deviceListView = new BluetoothDeviceListView() { };
        private ListView deviceListView = new ListView() {
            VerticalPadding = 44f
        };
        //private FramedView setupView = new FramedView() {
        //    Title = "looking for devices...",
        //    Footer = "Make sure your device is switched on and within range. Don't have one yet? Tap here to visit our website!",
        //};
        private NavigationView setupView = new NavigationView();
        private RemoteControlView remoteControlView = new RemoteControlView() {
            P = 0.2f, I = 0.4f, D = 0.6f, ILimit = 0.8f
        };
        private PlotView plotView = new PlotView() {
            Padding = new Margin(10, 30)
        };

        
        private GravityNegotiationDirect device;

        /// <summary>
        /// This routine should only be used to set up the initial GUI
        /// </summary>
        public Application()
        {
            mainWindow = new Window(topLayer);
            mainWindow.SetFocus();
            topLayer.Insert(setupView, true);



            if (!System.IO.File.Exists(Platform.AssetsPath + "/Firmware/Baseband.img"))
                Platform.DefaultLog.Log("firmware not found");

            NavigationBarItem i1 = new NavigationBarItem("lol");
            NavigationBarItem i2 = new NavigationBarItem(NavigationBarItem.iOSNavigationBarItemType.Action);
            //i2.Engaged += (o) => Platform.RootViewController.SetView(remoteControlView, AnimationType.Push, Direction.ToTheLeft());
            i2.Engaged += (o) => topLayer.Replace(setupView, remoteControlView, false, true, new Vector2D<float>(-1, 0));

            setupView.NavigateForward(deviceListView, "looking for devices...", false, i1, i2);
            


            // todo: add footer
            //setupView.TitleClicked += (o, e) => Platform.RootViewController.SetView(remoteControlView, AnimationType.Push, Direction.ToTheLeft());
            //setupView.FooterClicked += (o, e) => Platform.OpenWWW("http://" + AppInstall.Organization.GlobalConstants.WEB_SERVER);


            remoteControlView.ThrottleChanged += (o, e) => { if (device != null) { device.Throttle = e; device.Control(); } };
            remoteControlView.ConfigurationChanged += (o) => { if (device != null) device.Configure(remoteControlView.P, remoteControlView.I, remoteControlView.D, remoteControlView.ILimit, remoteControlView.A, remoteControlView.T); };
            remoteControlView.LayoutSubviews();
            remoteControlView.P = 0.2f;
            remoteControlView.I = 0f;
            remoteControlView.D = 0f;
            remoteControlView.ILimit = 0f;
            remoteControlView.A = 30f;
            remoteControlView.T = 0.150f;


            remoteControlView.SupplementaryAction += (o) => {
                plotView.Clear();
                if (device == null) {
                    plotView.Plot(new float[] { 1, 2, 6, 1, 6, 4, 9, 3, 6, 3 }, Color.Green);
                } else {
                    device.Throttle = 0.01f;
                    device.Control().WaitOne();
                    global::System.Threading.Thread.Sleep(500);
                    device.Throttle = 0.0f;
                    device.Control().WaitOne();
                    device.ReadLog();
                    plotView.Plot(device.PitchSensorLog, Color.Green);
                    plotView.Plot(device.PitchActionLog, Color.Magenta);
                }
                plotView.UpdateLayout();
                topLayer.Insert(plotView, false, new Vector2D<float>(1, 0));
            };

        }

        /// <summary>
        /// Main thread
        /// </summary>
        public void Main()
        {
            Platform.DefaultLog.Log("initializing update system...");
            InstallerSystem updater = new InstallerSystem();
            updater.Init(10, true, Platform.DefaultLog.SubContext("installer system"), ApplicationControl.ShutdownToken);

            Platform.DefaultLog.Log("loading bluetooth central...");
            CollectionSource<BluetoothPeripheral> client = BluetoothCentral.CreateDeviceSource();

            Platform.DefaultLog.Log("loading UI...");


            Platform.InvokeMainThread(() => {
                Window win = null;




                var mainView = new ListViewController<BluetoothPeripheral>() {
                    Data = client,
                    Title = "Devices",
                    Footer = "make sure that the device is switched on and in range",
                    RefreshText = "pull to scan",
                    RefreshingText = "scanning...",
                    Fields = new Field<BluetoothPeripheral>[] {
                        new TextFieldView<BluetoothPeripheral>("Name", p => p.Name, true),
                        new TextFieldView<BluetoothPeripheral>("signal strength", p => p.RSSI.ToString(), false)
                    },
                    DetailViewConstructor = (parent, peripheral, isNew) => new CustomViewController<BluetoothPeripheral> {
                        // todo: pull existing CustomViewController class into C# framework
                    }
                }




                Platform.DefaultLog.Log("creating window...");
                win = new Window(mainView);
                win.Show();
                win.Closed += () => ApplicationControl.Shutdown();

                new Task(() => {
                    while (!ApplicationControl.ShutdownToken.WaitHandle.WaitOne(10000)) {
                        Platform.DefaultLog.Log("UI dump:");
                        win.DumpLayout(Platform.DefaultLog);
                    }
                }).Start();
            });
        }



        private void bluetooth_StateChanged(BluetoothCentralState state)
        {
            Platform.InvokeMainThread(delegate {
                deviceListView.Clear();

                deviceListView.UpdateLayout();

                switch (state) {
                    case BluetoothCentralState.Ready:
                        //Platform.RootViewController.SetView(setupView, AnimationType.Fade, null);
                        topLayer.Remove(errView, true);
                        mainWindow.FrameColor = Color.Black;
                        //Platform.RootViewController.SetView(remoteControlView, AnimationType.Fade, null);
                        return;

                    case BluetoothCentralState.Unsupported:
                        errView.Message = "This device does not have Bluetooth v4.0. You need an iPhone 4S, iPad 3, iPod Touch 5 or newer.\n\n" +
                                          "Future support is planned for these devices: MacBook Air 2011, MacBook Pro 2012, Mac Mini";
                        break;

                    case BluetoothCentralState.Unauthorized:
                        errView.Message = "Please allow this App to use Bluetooth";
                        break;

                    case BluetoothCentralState.Off:
                        errView.Message = "Please go to Settings and turn on Bluetooth";
                        break;

                    default:
                        errView.Message = "Unknown bluetooth state. Please contact support.";
                        break;
                }

                //Platform.RootViewController.SetView(errView, AnimationType.Fade, null);
                topLayer.Insert(errView, true);
                mainWindow.FrameColor = Color.White;
            });

            //if (bluetooth.State == BluetoothCentralState.Ready) bluetooth.StartScan(new Guid[] { new Guid(AppInstall.Organization.GlobalConstants.FLIGHT_SERVICE_UUID) });
            if (state == BluetoothCentralState.Ready) BluetoothCentral.StartScan(null);
        }


        private void device_identify(object o, object e)
        {
            BluetoothPeripheral dev = (BluetoothPeripheral)e;

            new Task(delegate {
                try {
                    Platform.DefaultLog.Log("connecting...");
                    BluetoothCentral.ConnectPeripheral(dev);
                    Platform.DefaultLog.Log("connected");
                    dev.RetrieveDetails();
                    if (BluetoothUpdateManager.ShouldUpdate(dev, (d) => null)) {
                        Platform.DefaultLog.Log("needs update");

                        ProgressObserver p = new ProgressObserver() { MinimumStep = 0.01 };
                        p.ProgressChanged += (o2, e2) => Platform.DefaultLog.Log("at " + Math.Round(e2 * 100) + "%");
                        p.StatusChanged += (o2, e2) => Platform.DefaultLog.Log("upload: " + e2);

                        using (ProgressView pv = Platform.InvokeMainThread(() => new ProgressView("Updating Device Firmware", "A firmware update is being transferred to your device. You may abort the process at any time without breaking the device.", p))) {
                            Platform.InvokeMainThread(pv.Show, true);
                            BluetoothUpdateManager.Update(dev, new CSRImage(Platform.AssetsPath + "/Firmware/Baseband.img"), p);
                        }

                        Platform.MsgBox("the new firmware was uploaded successfully", "update complete");
                    } else {
                        //LogSystem.Log("unable to update or no update required");
                        //Platform.MsgBox("the device already has the latest firmware", "no update required");
                    }


                    device = new GravityNegotiationDirect(new BluetoothI2CProxy(dev));
                    topLayer.Replace(setupView, remoteControlView, false, true, new Vector2D<float>(-1, 0));

                    //while (true) {
                    //    //device.ReadData();
                    //    //LogSystem.Log("attitude: " + device.Attitude + ", angular rate: " + device.AngularRate);
                    //    //global::System.Threading.Thread.Sleep(1000);
                    //    //LogSystem.Log("launching");
                    //    //device.Throttle = 0.5f;
                    //    //device.Control();
                    //    //global::System.Threading.Thread.Sleep(500);
                    //    //LogSystem.Log("shutting down");
                    //    //device.Throttle = 0;
                    //    //device.Control();
                    //    //device.Control();
                    //}


                } catch (Exception ex) {
                    Platform.DefaultLog.Log("connection failed: " + ex.ToString());
                    Platform.MsgBox(ex.Message, "connection failed");
                }
            }).Start();

            // selected: {
            //    new Task(delegate {
            //        try {
            //            LogSystem.Log("connecting...");
            //            bluetooth.ConnectPeripheral(e);
            //            // todo: disconnect
            //        } catch (Exception ex) {
            //            LogSystem.Log("connection failed: " + ex.ToString());
            //            Platform.MsgBox(ex.Message, "device damaged");
            //        }
            //    }).Start();
            //};
        }
    }
}