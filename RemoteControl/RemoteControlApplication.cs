using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
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
using AppInstall.Networking;


namespace AppInstall
{

    /// <summary>
    /// Represents a collection of all services supported by this application.
    /// A device does not have to support all of these services.
    /// </summary>
    class RemoteDevice
    {
        public readonly ZeroConfService network;
        public readonly BluetoothPeripheral bluetooth;

        public string Name
        {
            get
            {
                if (network == null)
                    return network.Name;
                else
                    return bluetooth.Name;
            }
        }

        /// <summary>
        /// Service for controlling the device's immediate actions
        /// </summary>
        public readonly FlightControllerService flightControllerService;


        public RemoteDevice(ZeroConfService network, BluetoothPeripheral bluetooth)
        {
            this.network = network;
            this.bluetooth = bluetooth;

            flightControllerService = new FlightControllerService(network, bluetooth, Platform.DefaultLog.SubContext("flight control service"));
        }
    }


    /// <summary>
    /// Platform independent implementation of the Remote Control application
    /// </summary>
    class Application
    {
        static LogContext uiLog = Platform.DefaultLog.SubContext("UI");
        public static LogContext UILog { get { return uiLog; } }

        public static string ApplicationName { get { return "Remote Control"; } }
        public static Color ThemeColor { get { return GlobalConstants.UIColor; } }
        
        

        
        private FlightControllerService device;

        /// <summary>
        /// This routine should only be used to set up the initial GUI
        /// </summary>
        public Application(string[] args)
        {
        }

        // legacy pre-build event: rmdir /S /Q \\psf\home\Library\Caches\Xamarin\mtbs
        // rmdir /S /Q \\psf\home\Library\Caches\Xamarin\mtbs
        // rmdir /S /Q C:\Data\Projects\s1\remotecontrol.ios\bin
        // rmdir /S /Q C:\Data\Projects\s1\remotecontrol.ios\obj

        /// <summary>
        /// Main thread
        /// </summary>
        public void Main()
        {
            Platform.DefaultLog.Log("initializing update system...");
            InstallerSystem updater = new InstallerSystem();
            updater.Init(10, true, Platform.DefaultLog.SubContext("installer system"), ApplicationControl.ShutdownToken);

            Platform.DefaultLog.Log("loading bluetooth central...");
            BluetoothCentral.LogContext = Platform.DefaultLog.SubContext("BT");
            var bluetoothPeripherals = BluetoothCentral.CreateDeviceSource(null, true);
            var networkPeripherals = ZeroConf.CreateSource("_http._tcp", null, true);

            var list1 = new MappedSource<BluetoothPeripheral, RemoteDevice>(bluetoothPeripherals, dev => new RemoteDevice(null, dev), null, false);
            var list2 = new MappedSource<ZeroConfService, RemoteDevice>(networkPeripherals, dev => new RemoteDevice(dev, null), null, false);
            var listX = new JointSource<RemoteDevice>(list1, list2);


            Platform.DefaultLog.Log("loading UI...");


            Platform.InvokeMainThread(() => {
                Window win = null;




                var mainView = new ListViewController<RemoteDevice>() {
                    Data = listX,
                    Title = "Devices",
                    Footer = "make sure that the device is switched on and in range", // todo: implement
                    RefreshText = "pull to scan",
                    RefreshingText = "scanning...",
                    RefreshErrorMessageFactory = ex => ex.Message,
                    Fields = new Field<RemoteDevice>[] {
                        new TextFieldView<RemoteDevice>("Name", p => p.Name == null ? null : p.Name, true),
                        new TextFieldView<RemoteDevice>("signal strength", p => p.bluetooth == null ? "N/A" : p.bluetooth.RSSI.ToString(), false)
                    },
                    DetailViewConstructor = (parent, peripheral, isNew) => new CustomViewController<RemoteDevice> {
                        Title = "Remote Control",
                        Data = new DataSource<RemoteDevice>(peripheral),
                        ViewConstructor = () => {

                            var remoteControlView = new RemoteControlView() {
                                P = 0.2f,
                                I = 0.4f,
                                D = 0.6f,
                                ILimit = 0.8f
                            };

                            remoteControlView.ThrottleChanged += (o, e) => { if (device != null) { device.Throttle = e; device.Control(); } };
                            remoteControlView.ConfigurationChanged += (o) => { if (device != null) device.Configure(remoteControlView.P, remoteControlView.I, remoteControlView.D, remoteControlView.ILimit, remoteControlView.A, remoteControlView.T); };
                            remoteControlView.UpdateLayout();
                            remoteControlView.P = 0.2f;
                            remoteControlView.I = 0f;
                            remoteControlView.D = 0f;
                            remoteControlView.ILimit = 0f;
                            remoteControlView.A = 30f;
                            remoteControlView.T = 0.150f;

                            remoteControlView.SupplementaryAction += (o) => {

                            };

                            return remoteControlView;
                        },
                        Features = new List<FeatureController>() {
                            new DialogFeature<BluetoothPeripheral>() {
                                Text = "plot",
                                ViewConstructor = obj => new CustomViewController<BluetoothPeripheral> {
                                    Title = "plot",
                                    ViewConstructor = () => {
                                        var plotView = new PlotView() {
                                            Padding = new Margin(10, 30)
                                        };

                                        if (device == null) {
                                            plotView.Plot(new float[] { 1, 2, 6, 1, 6, 4, 9, 3, 6, 3 }, Color.Green);
                                        } else {
                                            device.Throttle = 0.01f;
                                            device.Control().WaitOne();
                                            System.Threading.Thread.Sleep(500);
                                            device.Throttle = 0.0f;
                                            device.Control().WaitOne();
                                            //device.ReadLog();
                                            plotView.Plot(device.PitchSensorLog, Color.Green);
                                            plotView.Plot(device.PitchActionLog, Color.Magenta);
                                        }

                                        return plotView;
                                    }
                                }
                            }
                        }
                    }
                };




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


                /*


                var logB = Platform.DefaultLog.SubContext("bttest");


                var _serviceList = new List<Foundation.NSNetService>();
                var _netBrowser = new Foundation.NSNetServiceBrowser();

                EventHandler serviceAddressResolved = (sender, e) => {
                    var ns = sender as Foundation.NSNetService;

                    if (ns != null)
                        logB.Log(ns.Name + " resolved");

                    // at this point you could use any networking code you like to communicate with the service
                };


                _netBrowser.SearchForServices("_http._tcp", "");

                _netBrowser.FoundService += delegate(object sender, Foundation.NSNetServiceEventArgs e) {
                    logB.Log(e.Service.Name + " added");

                    _serviceList.Add(e.Service);
                    e.Service.AddressResolved += serviceAddressResolved;
                };

                _netBrowser.ServiceRemoved += delegate(object sender, Foundation.NSNetServiceEventArgs e) {
                    logB.Log(e.Service.Name + " removed");

                    var nsService = _serviceList.Single(s => s.Name.Equals(e.Service.Name));
                    _serviceList.Remove(nsService);
                };
                */
            });
        }



        /*
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

                        using (ProgressView pv = Platform.EvaluateOnMainThread(() => new ProgressView("Updating Device Firmware", "A firmware update is being transferred to your device. You may abort the process at any time without breaking the device.", p))) {
                            Platform.InvokeMainThread(pv.Show, true);
                            BluetoothUpdateManager.Update(dev, new CSRImage(Platform.AssetsPath + "/Firmware/Baseband.img"), p);
                        }

                        Platform.MsgBox("the new firmware was uploaded successfully", "update complete");
                    } else {
                        //LogSystem.Log("unable to update or no update required");
                        //Platform.MsgBox("the device already has the latest firmware", "no update required");
                    }


                    device = new FlightControllerEndpoint(new BluetoothI2CProxy(dev));
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
         */
    }
}