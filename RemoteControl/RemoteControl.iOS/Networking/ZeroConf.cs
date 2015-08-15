using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Foundation;
using CoreFoundation;
using AppInstall.Framework;
using AppInstall.OS;


namespace AppInstall.Networking
{
    /// <summary>
    /// Provides a platform independent interface to Bonjour (aka ZeroConf) service discovery and advertising.
    /// todo: separate platform dependend code from independend code and provide alternative implementations (use Mono.ZeroConf for windows/linux)
    /// </summary>
    public static class ZeroConf
    {



        private class BrowserDelegate : NSNetServiceBrowserDelegate
        {
            public event Action SearchStarted;
            public event Action SearchStopped;
            public event Action DidNotSearch;

            public override void FoundService(NSNetServiceBrowser sender, NSNetService service, bool moreComing)
            {
                base.FoundService(sender, service, moreComing);
                Platform.DefaultLog.Log("found service");
            }

            public override void ServiceRemoved(NSNetServiceBrowser sender, NSNetService service, bool moreComing)
            {
                base.ServiceRemoved(sender, service, moreComing);
                Platform.DefaultLog.Log("service removed");
            }

            public override void NotSearched(NSNetServiceBrowser sender, NSDictionary errors)
            {
                base.NotSearched(sender, errors);
                Platform.DefaultLog.Log("did not search");
                DidNotSearch.SafeInvoke();
            }
            
        }



        /// <summary>
        /// Returns a collection source that scans for some type of service on the network using the Bonjour protocol.
        /// </summary>
        /// <param name="service">a service identifier (e.g. "_http._tcp.")</param>
        /// <param name="txtFilter">A function that takes a service's TXT record and decides whether it should be accepted. If null, all instances of the service are accepted.</param>
        public static CollectionSource<ZeroConfService> CreateSource(string service, Func<Dictionary<string, string>, bool> txtFilter, bool refreshOnMainThread)
        {

            var browser = new NSNetServiceBrowser();
            //var del = new BrowserDelegate();
            //browser.Delegate = del;
            //browser.Schedule(NSRunLoop.Current, NSRunLoop.NSDefaultRunLoopMode);

            var doneSignal = new ManualResetEvent(false);
            Exception exception = null;

            var result = new CollectionSource<ZeroConfService>(c => {
                doneSignal.Reset();
                //browser.SearchForServices(service, "local");
                doneSignal.WaitOne(c);
                browser.Stop();
                if (exception != null)
                    throw exception;
                return new ZeroConfService[0];
            }, null, refreshOnMainThread, null);


            /*
            browser.SearchStarted += (o, e) =>
                result.SoftRefresh(ApplicationControl.ShutdownToken).Run();
            browser.SearchStopped += (o, e) => {
                exception = null;
                doneSignal.Set();
            };
            browser.NotSearched += (o, e) => {
                exception = e.ToException();
                doneSignal.Set();
            }; // todo: pass exception
             */


            var newServices = new List<NSNetService>();
            var oldServices = new List<NSNetService>();


            browser.SearchForServices(service, "");


            browser.FoundService += (o, e) => {
                if (txtFilter == null ? true : txtFilter(ZeroConfService.GetTxtRecord(e.Service)))
                    newServices.Add(e.Service);

                if (!e.MoreComing) {
                    Action action = () => result.AddRange(newServices.Select(s => new ZeroConfService(s)));
                    if (refreshOnMainThread)
                        Platform.InvokeMainThread(action);
                    else
                        action();
                    newServices.Clear();
                }
            };

            browser.ServiceRemoved += (o, e) => {
                oldServices.Add(e.Service);
                
                if (!e.MoreComing) {
                    Action action = () => result.RemoveAll(s => oldServices.Contains(s.nativeService));
                    if (refreshOnMainThread)
                        Platform.InvokeMainThread(action);
                    else
                        action();
                    oldServices.Clear();
                }
            };

            //browser.Schedule(Platform.BackgroundRunLoop, NSRunLoop.NSDefaultRunLoopMode);

            //result.Refresh(ApplicationControl.ShutdownToken).Run();
            //NSRunLoop.Current.Run();
















            

                var logB = Platform.DefaultLog.SubContext("bttest2");


                var _serviceList = new List<Foundation.NSNetService>();
                var _netBrowser = new Foundation.NSNetServiceBrowser();

                EventHandler serviceAddressResolved = (sender, e) => {
                    var ns = sender as Foundation.NSNetService;

                    if (ns != null)
                        logB.Log(ns.Name + " resolved");

                    // at this point you could use any networking code you like to communicate with the service
                };

                Platform.InvokeMainThread(() => {
                _netBrowser.SearchForServices("_http._tcp", "");
            });

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


            return result;
        }
    }


    public class ZeroConfService
    {
        private readonly bool isServer;


        public string Protocol { get; set; }

        public readonly NSNetService nativeService;
        public ZeroConfService(NSNetService service)
        {
            nativeService = service;
            isServer = false;



            Exception resolveException = null;
            var resolveCompleted = new AutoResetEvent(false);
            var resolveFailed = new AutoResetEvent(false);
            var resolving = false;

            nativeService.AddressResolved += (o, e) => {
                Application.UILog.Log("addresses: " + nativeService.Addresses);
                resolving = false;
                addresses = nativeService.Addresses.Select(address => (System.Net.IPAddress)null).ToArray(); // todo: convert
                if (!addresses.Any())
                    resolveException = new Exception("can't resolve address for " + nativeService.HostName);
                else
                    resolveException = null;
                resolveCompleted.Set();
            };

            nativeService.ResolveFailure += (o, e) => {
                resolving = false;
                resolveException = e.ToException();
                resolveCompleted.Set();
            };

            resolveAction = new SlowAction(cancellationToken => {
                if (!resolving) {
                    resolving = true;
                    nativeService.Resolve();
                }

                resolveCompleted.WaitOne(cancellationToken);
                
                if (resolveException != null)
                    throw resolveException;
            });
        }

        public string Name
        {
            get
            {
                return nativeService.Name;
            }
            set
            {
                if (!isServer)
                    throw new InvalidOperationException("The name cannot be changed by the client");
                throw new NotImplementedException();
                //nativeService.Name = value;
            }
        }


        public Dictionary<string, string> TXTRecords
        {
            get
            {
                return GetTxtRecord(nativeService);
            }
        }

        
        private System.Net.IPAddress[] addresses = new System.Net.IPAddress[0];
        private object lockRef = new object();
        SlowAction resolveAction;

        /// <summary>
        /// Resolves the host address.
        /// </summary>
        public async Task Resolve(CancellationToken cancellationToken)
        {
            await resolveAction.SoftTriggerAndWait(cancellationToken);
        }


        /// <summary>
        /// Connects to the service using the advertised link layer protocol and port.
        /// The service must be resolved first. If multiple addresses are available, only one address is used.
        /// The returned socket is ready to send and receive data and should be disposed when done.
        /// </summary>
        public Socket Connect()
        {
            var address = addresses.FirstOrDefault(a => a != null);
            if (address == null)
                throw new InvalidOperationException("No host address available. Make sure to successfully resolve the service before connceting to it.");

            Socket socket;

            if (nativeService.Name.Contains("._tcp.")) // todo: verify or use cleaner method
                socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            else if (nativeService.Name.Contains("._udp."))
                socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            else
                throw new FormatException("unknown protocol: " + nativeService.Name);

            try {
                socket.Connect(address, (int)nativeService.Port);
                if (!socket.Connected)
                    throw new Exception("unexpected connection error");
            } catch {
                socket.Dispose();
                throw;
            }

            return socket;
        }


        public static Dictionary<string, string> GetTxtRecord(NSNetService service)
        {
            if (service.TxtRecordData == null)
                return new Dictionary<string, string>();
            return NSNetService.DictionaryFromTxtRecord(service.TxtRecordData).ToDictionary(val => val.Key.ToString(), val => val.Value.ToString());
        }
        
    }

    public static class NetExtenstions {
        public static Exception ToException(this NSNetServiceErrorEventArgs error)
        {
            return new NSError((NSString)error.Errors.ObjectForKey(new NSString("NSNetServicesErrorDomain")), (int)(NSNumber)error.Errors.ObjectForKey(new NSString("NSNetServicesErrorCode"))).ToException();
        }
    }
}