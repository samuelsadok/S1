using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AppInstall.Framework;
using AppInstall.Networking;

namespace AppInstall.Hardware
{

    /// <summary>
    /// A service represents a collection of endpoints.
    /// An endpoint is usually a single value, data block or stream.
    /// </summary>
    public abstract class Service
    {

        protected readonly LogContext logContext;

        private ZeroConfService network;
        private BluetoothPeripheral peripheral;

        private string serviceName;
        private Guid serviceGuid;


        /// <summary>
        /// Creates a service that is either accessed via network or bluetooth.
        /// </summary>
        /// <param name="name">A string that identifies the service. Must not contain special characters. Used if the service is accessed via network.</param>
        /// <param name="guid">A GUID that identifies the service. Used if the service is accessed via bluetooth.</param>
        /// <param name="network">The ZeroConf service for access via network. If not null, network access is used.</param>
        /// <param name="bluetooth">A bluetooth peripheral that is used for service access via bluetooth. Must not be null if socket is null.</param>
        public Service(string name, Guid guid, ZeroConfService network, BluetoothPeripheral bluetooth, LogContext logContext)
        {
            if (network == null && bluetooth == null)
                throw new ArgumentNullException("socket & peripheral");
            this.serviceName = name;
            this.serviceGuid = guid;
            this.network = network;
            this.peripheral = bluetooth;
            this.logContext = logContext;
        }


        /// <summary>
        /// Writes a value to an endpoint.
        /// </summary>
        /// <param name="name">Endpoint name used for network access</param>
        /// <param name="guid">Characteristic guid used for bluetooth access</param>
        protected async Task WriteEndpoint(string name, Guid guid, byte[] value, CancellationToken cancellationToken)
        {
            if (network != null) {

                throw new NotImplementedException();

            } else {

                await peripheral.WriteCharacteristic(serviceGuid, guid, value, cancellationToken);

            }
        }

        /// <summary>
        /// Reads a value from an endpoint.
        /// </summary>
        /// <param name="name">Endpoint name used for network access</param>
        /// <param name="guid">Characteristic guid used for bluetooth access</param>
        protected async Task<byte[]> ReadEndpoint(string name, Guid guid, CancellationToken cancellationToken)
        {
            if (network != null) {

                throw new NotImplementedException();

            } else {

                return await peripheral.ReadCharacteristic(serviceGuid, guid, cancellationToken);

            }
        }

        /// <summary>
        /// Registers a handler that will typically be called when the endpoint value changes.
        /// The exact behaviour depends on the remote host.
        /// The remote host decides whether to use a reliable or unreliable event delivery method.
        /// In Bluetooth, indications require an acknowledgement, while notifications are typically used for
        /// endpoints that change frequently.
        /// </summary>
        /// <param name="name">Endpoint name used for network access</param>
        /// <param name="guid">Characteristic guid used for bluetooth access</param>
        /// <param name="handler">A handler that will be invoked when the endpoint value changes</param>
        protected void SubscribeToEndpoint(string name, Guid guid, Action<byte[]> handler)
        {
            if (network != null) {

                throw new NotImplementedException();

            } else {

                peripheral.EnableNotifications(serviceGuid, guid);

            }
        }
    }
}