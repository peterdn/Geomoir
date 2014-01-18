using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Geomoir.Bluetooth
{
    class ClientConnectedEventArgs
    {
        public BluetoothDevice Device { get; private set; }
        public BluetoothDuplexConnection Connection { get; private set; }

        public ClientConnectedEventArgs(BluetoothDevice Device, BluetoothDuplexConnection Connection)
        {
            this.Device = Device;
            this.Connection = Connection;
        }
    }

    class BluetoothServer
    {
        public enum BluetoothServerState
        {
            Stopped,
            Advertising,
            Connected,
            Faulty
        }
        
        private RfcommServiceProvider _serviceProvider;
        public Guid ServiceID { get; private set; }

        public event TypedEventHandler<BluetoothServer, BluetoothServerState> StateChanged;
        public event TypedEventHandler<BluetoothServer, ClientConnectedEventArgs> ClientConnected; 

        private StreamSocket _socket;
        private DataWriter _writer;
        private DataReader _reader;

        private BluetoothServerState _serverState;
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
                {
                    StateChanged(this, _serverState);
                }
            }
        }

        public BluetoothServer(Guid ServiceID)
        {
            this.ServiceID = ServiceID;

            this.ServerState = BluetoothServerState.Stopped;
        }

        public async Task Start()
        {
            try
            {
                if (_serviceProvider == null)
                {
                    _serviceProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(ServiceID));
                }
                var listener = new StreamSocketListener();
                listener.ConnectionReceived += ListenerOnConnectionReceived;

                await listener.BindServiceNameAsync(_serviceProvider.ServiceId.AsString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                
                // There doesn't seem to be much point to writing SDP
                // records as there is no way to retrieve them in WP8?
                //var sdpRecord = new DataWriter();
                //sdpRecord.WriteByte((4 << 3) | 5);
                //sdpRecord.WriteByte((byte)ServiceName.Length);

                //sdpRecord.UnicodeEncoding = UnicodeEncoding.Utf8;
                //sdpRecord.WriteString(ServiceName);

                //await sdpRecord.StoreAsync();

                //if (_serviceProvider.SdpRawAttributes.ContainsKey(0x100))
                //{
                //    _serviceProvider.SdpRawAttributes.Remove(0x100);
                //}
                    
                //_serviceProvider.SdpRawAttributes.Add(0x100, sdpRecord.DetachBuffer());

                _serviceProvider.StartAdvertising(listener);
            } 
            catch (Exception ex)
            {
                
            }

            ServerState = BluetoothServerState.Advertising;
        }

        public void Stop()
        {
            if (ServerState != BluetoothServerState.Advertising)
            {
                throw new InvalidOperationException("The server is not currently advertising");
            }

            _serviceProvider.StopAdvertising();
            ServerState = BluetoothServerState.Stopped;
        }

        public void Disconnect()
        {
            if (ServerState != BluetoothServerState.Connected)
            {
                throw new InvalidOperationException("The server is not currently connected");
            }

            ServerState = BluetoothServerState.Stopped;
        }

        private void ListenerOnConnectionReceived(StreamSocketListener Listener, StreamSocketListenerConnectionReceivedEventArgs Args)
        {
            // Only supports a single connection for the moment so we won't need these again
            _serviceProvider.StopAdvertising();
            Listener.Dispose();
            
            _socket = Args.Socket;
            _writer = new DataWriter(_socket.OutputStream);
            _reader = new DataReader(_socket.InputStream);

            var deviceName = "";
            var serviceName = "";
            HostName hostName = null;
            if (_socket.Information != null)
            {
                // Turns out this really only gives us the host
                // address and an arbitrary number. Pretty useless.
                deviceName = _socket.Information.RemoteHostName.DisplayName;
                hostName = _socket.Information.RemoteHostName;
                serviceName = _socket.Information.RemoteServiceName;
            }
            
            ServerState = BluetoothServerState.Connected;
            if (ClientConnected != null)
            {
                var device = new BluetoothDevice(deviceName, hostName, serviceName);
                var connection = new BluetoothDuplexConnection(_reader, _writer);
                ClientConnected(this, new ClientConnectedEventArgs(device, connection));
            }
        }

        private void AssertConnected()
        {
            if (ServerState != BluetoothServerState.Connected)
            {
                throw new InvalidOperationException("Server is not connected");
            }
        }

        private async void Sync(StreamSocket Socket)
        {
            if (Socket != null)
            {
                var reader = new DataReader(Socket.InputStream);

                var read = await reader.LoadAsync(sizeof (UInt32));
                var dataLength = reader.ReadUInt32();

                read = await reader.LoadAsync(dataLength);
                var data = reader.ReadString(dataLength);

                reader.Dispose();
                Socket.Dispose();
            }
        }
    }

    internal class StateChangedEventArgs
    {
        public BluetoothServer.BluetoothServerState OldServerState { get; set; }
        public BluetoothServer.BluetoothServerState ServerState { get; set; }
    }
}
