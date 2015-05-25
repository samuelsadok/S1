using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MonoTouch.Foundation;
using MonoTouch.CoreBluetooth;
using AppInstall.Framework;

namespace AppInstall.Hardware
{
    /// <summary>
    /// Provides functions to use bluetooth low energy on the platform.
    /// All public functions except Init() are thread safe.
    /// </summary>
    public static class BluetoothCentral
    {
        static VolatileList<CBPeripheral> peripherals = new VolatileList<CBPeripheral>(TimeSpan.FromSeconds(5));
        static CBCentralManager central = new CBCentralManager(new MonoTouch.CoreFoundation.DispatchQueue("bluetooth dispatch queue"));
        static Dictionary<CBPeripheral, BluetoothPeripheral> availablePeripherals = new Dictionary<CBPeripheral, BluetoothPeripheral>();
        static Dictionary<CBPeripheral, Action<NSError>> connectionAttemptDoneHandler = new Dictionary<CBPeripheral, Action<NSError>>();


        /// <summary>
        /// The state of the central has changed. If a scan was in progress it will be stopped.
        /// </summary>
        public static event Action<BluetoothCentralState> StateChanged;
        /// <summary>
        /// A new peripheral was discovered
        /// </summary>
        public static event Action<BluetoothPeripheral> DiscoveredPeripheral;
        /// <summary>
        /// A discovered peripheral was lost
        /// </summary>
        public static event Action<BluetoothPeripheral> LostPeripheral;
        /// <summary>
        /// The bluetooth central started to connect
        /// </summary>
        public static event Action<BluetoothPeripheral> ConnectionInitiated;
        /// <summary>
        /// The device was paired successfully
        /// </summary>
        public static event Action<BluetoothPeripheral> ConnectionEstablished;
        /// <summary>
        /// The connection to the device was closed. Also triggered when a connection attempt failed.
        /// </summary>
        public static event Action<BluetoothPeripheral> ConnectionClosed;


        public static BluetoothCentralState State
        {
            get
            {
                switch (central.State) {
                    case CBCentralManagerState.Unsupported: return BluetoothCentralState.Unsupported;
                    case CBCentralManagerState.Unauthorized: return BluetoothCentralState.Unauthorized;
                    case CBCentralManagerState.PoweredOff: return BluetoothCentralState.Off;
                    case CBCentralManagerState.PoweredOn: return BluetoothCentralState.Ready;
                }
                return BluetoothCentralState.Unknown;
            }
        }


        /// <summary>
        /// Initializes the bluetooth central.
        /// Until this function is called, bluetooth central functions are not operational.
        /// </summary>
        public static void Init()
        {
            // init static variables
            peripherals = new VolatileList<CBPeripheral>(TimeSpan.FromSeconds(5));
            central = new CBCentralManager(new MonoTouch.CoreFoundation.DispatchQueue("bluetooth dispatch queue"));
            availablePeripherals = new Dictionary<CBPeripheral, BluetoothPeripheral>();
            connectionAttemptDoneHandler = new Dictionary<CBPeripheral, Action<NSError>>();



            Action<string> bluetoothHandler = (eventName) => LogSystem.Log("BT: " + eventName);

            central.UpdatedState += (o, e) => {
                Console.WriteLine("BT: state update");
                //ClearAvailableDevices();
                peripherals.Clear();
                StateChanged.SafeInvoke(State);
            };
            central.DiscoveredPeripheral += (o, e) => central_DiscoveredPeripheral(e.Peripheral, e.AdvertisementData, (int)e.RSSI);
            central.ConnectedPeripheral += (o, e) => central_ConnectPeripheralDone(e.Peripheral, null);
            central.FailedToConnectPeripheral += (o, e) => central_ConnectPeripheralDone(e.Peripheral, e.Error);
            central.DisconnectedPeripheral += (o, e) => central_DisconnectedPeripheral(e.Peripheral, e.Error);
            central.RetrievedPeripherals += (o, e) => bluetoothHandler("retrieved peripherals");
            central.RetrievedConnectedPeripherals += (o, e) => bluetoothHandler("retrieved connected peripherals");



            peripherals.FoundObject += (o, e) => {
                BluetoothPeripheral p = new BluetoothPeripheral(e.Item1, (NSDictionary)e.Item2[0], (int)e.Item2[1]);
                availablePeripherals.Add(e.Item1, p);
                DiscoveredPeripheral.SafeInvoke(p);
            };
            peripherals.TouchedObject += (o, e) => availablePeripherals[e.Item1].UpdateData((NSDictionary)e.Item2[0], (int)e.Item2[1]); ;
            peripherals.LostObject += (o, e) => {
                BluetoothPeripheral p = availablePeripherals[e];
                availablePeripherals.Remove(e);
                LostPeripheral.SafeInvoke(p);
            };
            peripherals.StartMonitoring(new CancellationToken());
        }


        public static CollectionSource<BluetoothPeripheral> CreateDeviceSource()
        {
            BluetoothCentral.StateChanged += bluetooth_StateChanged;
            BluetoothCentral.DiscoveredPeripheral += (p) => Platform.InvokeMainThread(delegate { deviceListView.AppendItem(p, true, device_identify); });
            BluetoothCentral.LostPeripheral += (p) => Platform.InvokeMainThread(delegate { deviceListView.RemoveItem(p, true); });
            BluetoothCentral.ConnectionInitiated += (p) => Platform.InvokeMainThread(deviceListView.UpdateLayout);
            BluetoothCentral.ConnectionEstablished += (p) => LogSystem.Log("ESTABLISHED!");
            BluetoothCentral.ConnectionClosed += (p) => { Platform.InvokeMainThread(deviceListView.UpdateLayout); topLayer.Replace(remoteControlView, setupView, false, true, new Vector2D<float>(-1, 0)); };
            BluetoothCentral.Init();
        }


        private static void central_DiscoveredPeripheral(CBPeripheral peripheral, NSDictionary advertismentData, int RSSI)
        {
            try {
                peripherals.Touch(peripheral, advertismentData, RSSI);
            } catch (ArgumentException) { // legitimate exception (means we don't have enough info about the device)
                return;
            }
        }

        private static void central_ConnectPeripheralDone(CBPeripheral peripheral, NSError error)
        {
            Console.WriteLine("BT: connected peripheral (or failed)");
            if (connectionAttemptDoneHandler.ContainsKey(peripheral)) connectionAttemptDoneHandler[peripheral](error);
        }

        private static void central_DisconnectedPeripheral(CBPeripheral peripheral, NSError error)
        {
            Console.WriteLine("BT: disconnected peripheral");
            availablePeripherals[peripheral].IsConnected = false;
            ConnectionClosed.SafeInvoke(availablePeripherals[peripheral]);
            peripherals.MakeVolatile(peripheral);
        }


        //private void ClearAvailableDevices()
        //{
        //    lock (availablePeripherals) {
        //        foreach (KeyValuePair<CBPeripheral, BluetoothPeripheral> kv in availablePeripherals)
        //            kv.Value.Dispose();
        //        availablePeripherals.Clear();
        //    }
        //}


        /// <summary>
        /// Scan for devices that expose one or more of the specified services
        /// </summary>
        public static void StartScan(Guid[] UUIDs)
        {
            central.ScanForPeripherals(((UUIDs == null) ? null : (from id in UUIDs select CBUUID.FromString(id.ToString())).ToArray()), new NSDictionary(CBCentralManager.ScanOptionAllowDuplicatesKey, true));
            Console.WriteLine("BT: started scan");
        }

        /// <summary>
        /// Stops the ongoing scan and removes all discovered peripherals from the list of available devices
        /// </summary>
        public static void StopScan()
        {
            central.StopScan();
            // ClearAvailableDevices();
            Console.WriteLine("BT: stopped scan");
        }

        /// <summary>
        /// Connects to a peripheral
        /// </summary>
        /// <exception cref="InvalidOperationException">The connection attempt failed</exception>
        /// <exception cref="TimeoutException">The connection attempt timed out</exception>
        public static void ConnectPeripheral(BluetoothPeripheral peripheral)
        {
            CBPeripheral p = peripheral.peripheral;

            peripherals.MakeResilent(p);

            lock (p) {
                peripheral.IsConnected = true;
                ConnectionInitiated.SafeInvoke(peripheral);

                try {
                    AutoResetEvent done = new AutoResetEvent(false);
                    NSError error = null;

                    connectionAttemptDoneHandler[p] = (err) => {
                        error = err;
                        done.Set();
                    };
                    
                    central.ConnectPeripheral(p);
                    if (!done.WaitOne(10000)) { central.CancelPeripheralConnection(p); throw new TimeoutException(); }

                    if (error != null) throw new InvalidOperationException();

                    ConnectionEstablished.SafeInvoke(peripheral);

                } catch (Exception) {
                    peripheral.IsConnected = false;
                    ConnectionClosed.SafeInvoke(peripheral);
                    peripherals.MakeVolatile(p);
                    throw;
                }
            }
        }

        public static void DisconnectPeripheral(BluetoothPeripheral peripheral)
        {

            // todo: disconnect

            lock (peripheral.peripheral) {
                Console.WriteLine("connection closing");
            }
        }
    }
}