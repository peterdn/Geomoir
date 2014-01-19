using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Geomoir.Bluetooth
{
    class BluetoothServer
    {
        #region Error codes

        // Bluetooth is currently disabled or not present.
        private const uint ERROR_BLUETOOTH_DISABLED = 0x80070490;

        // The application is missing the bluetooth.rfcomm capability in its manifest.
        private const uint ERROR_MISSING_CAPABILITIES = 0x80070005;

        #endregion

        /// <summary>
        /// Enumeration representing the current state of the server.
        /// </summary>
        public enum BluetoothServerState
        {
            /// <summary>
            /// The server is currently stopped.
            /// </summary>
            Stopped = 0,

            /// <summary>
            /// The server is currently advertising services
            /// and listening for incoming connections.
            /// </summary>
            Advertising = 1,

            /// <summary>
            /// The server is currently connected to a client.
            /// </summary>
            Connected = 2,

            /// <summary>
            /// The server encountered an error after a connection was established.
            /// </summary>
            Faulted = 4
        }
        
        private RfcommServiceProvider _serviceProvider;
        
        /// <summary>
        /// Unique identifier for the bluetooth service.
        /// </summary>
        public Guid ServiceID { get; private set; }

        #region Events

        /// <summary>
        /// Fired when the server state changes.
        /// </summary>
        public event TypedEventHandler<BluetoothServer, BluetoothServerState> StateChanged;

        /// <summary>
        /// Fired when a client connects to the server.
        /// </summary>
        public event TypedEventHandler<BluetoothServer, ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// Fired when a connection error occurs.
        /// </summary>
        public event TypedEventHandler<BluetoothServer, Exception> ConnectionError;

        #endregion

        private StreamSocket _socket;
        private DataWriter _writer;
        private DataReader _reader;

        
        /// <summary>
        /// The current state of the server.
        /// </summary>
        public BluetoothServerState ServerState
        {
            get
            {
                return _serverState;
            }
            private set
            {
                if (_serverState == value)
                    return;

                _serverState = value;
                    
                if (StateChanged != null)
                    StateChanged(this, _serverState);
            }
        }
        private BluetoothServerState _serverState;

        /// <summary>
        /// Creates an initializes a new instance of a bluetooth server.
        /// </summary>
        /// <param name="ServiceID">The unique identifier of the bluetooth service.</param>
        public BluetoothServer(Guid ServiceID)
        {
            this.ServiceID = ServiceID;
            this.ServerState = BluetoothServerState.Stopped;
        }

        /// <summary>
        /// Starts the server advertising its service over bluetooth.
        /// </summary>
        /// <returns>The asynchronous request.</returns>
        /// <exception cref="BluetoothDisabledException">
        /// Thrown when bluetooth is disabled or otherwise not available on the device.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Thrown when the application is missing the required capabilities in its manifest.
        /// </exception>
        public async Task Start()
        {
            try
            {
                if (_serviceProvider == null)
                    _serviceProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(ServiceID));

                var listener = new StreamSocketListener();
                listener.ConnectionReceived += ListenerOnConnectionReceived;

                await listener.BindServiceNameAsync(_serviceProvider.ServiceId.AsString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                
                _serviceProvider.StartAdvertising(listener);
            } 
            catch (Exception ex)
            {
                var errorCode = (uint) ex.HResult;
                
                ServerState = BluetoothServerState.Stopped;

                if (errorCode == ERROR_BLUETOOTH_DISABLED)
                    throw new BluetoothDisabledException();
               
                if (errorCode == ERROR_MISSING_CAPABILITIES)
                    throw new SecurityException("This application is missing the required capabilities in its manifest");

                throw;
            }

            ServerState = BluetoothServerState.Advertising;
        }

        /// <summary>
        /// Stops the server from advertising its bluetooth service.
        /// </summary>
        public void Stop()
        {
            // Stop() and Disconnect() could probably be rolled into a single method,
            // however they do have distinct *semantic* intents. A case could be made
            // for simple APIs, but keeping them separate at this stage makes it easier
            // to support multiple connections & advertising simultaneously in future.

            if (ServerState != BluetoothServerState.Advertising)
                throw new InvalidOperationException("The server is not currently advertising");
            
            _serviceProvider.StopAdvertising();
            ServerState = BluetoothServerState.Stopped;
        }

        /// <summary>
        /// Disconnects the server from a client.
        /// </summary>
        public void Disconnect()
        {
            if (ServerState != BluetoothServerState.Connected)
                throw new InvalidOperationException("The server is not currently connected");

            _reader.DetachStream();
            _writer.DetachStream();
            _socket.Dispose();

            ServerState = BluetoothServerState.Stopped;
        }

        private void ListenerOnConnectionReceived(StreamSocketListener Listener, StreamSocketListenerConnectionReceivedEventArgs Args)
        {
            var deviceName = "";
            var serviceName = "";
            HostName hostName = null;
            
            try
            {
                // Only supports a single connection for the moment so we won't need these again
                _serviceProvider.StopAdvertising();
                Listener.Dispose();

                _socket = Args.Socket;
                _writer = new DataWriter(_socket.OutputStream);
                _reader = new DataReader(_socket.InputStream);

                if (_socket.Information != null)
                {
                    // Turns out this only gives us the host address and an
                    // arbitrary number for service name. Pretty useless.
                    deviceName = _socket.Information.RemoteHostName.DisplayName;
                    hostName = _socket.Information.RemoteHostName;
                    serviceName = _socket.Information.RemoteServiceName;
                }
            } 
            catch (Exception ex)
            {
                ServerState = BluetoothServerState.Faulted;
                if (ConnectionError != null)
                    ConnectionError(this, ex);

                return;
            }

            ServerState = BluetoothServerState.Connected;
            if (ClientConnected != null)
            {
                var device = new BluetoothDevice(deviceName, hostName, serviceName);
                var connection = new DuplexConnection(_reader, _writer);
                ClientConnected(this, new ClientConnectedEventArgs(device, connection));
            }
        }

        private void AssertConnected()
        {
            if (ServerState != BluetoothServerState.Connected)
                throw new InvalidOperationException("The server is not currently connected");
        }
    }
}
