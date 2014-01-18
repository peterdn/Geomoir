using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Geomoir.Bluetooth;

namespace Geomoir_Tracker
{
    class BluetoothClient
    {
        public event TypedEventHandler<BluetoothClient, BluetoothDuplexConnection> ConnectionEstablished; 
        
        public static readonly Guid ServiceGUID = Guid.Parse("EEB35C6F-33DB-4DE3-8B4E-CFA313E92640");
        
        public static string ServiceName
        {
            get
            {
                return "{" + ServiceGUID.ToString() + "}";
            }
        }

        public async Task Connect()
        {
            PeerFinder.AlternateIdentities["Bluetooth:SDP"] = ServiceGUID.ToString();
            var peers = await PeerFinder.FindAllPeersAsync();

            foreach (var p in peers)
            {
                var h = p.ServiceName;
            }

            // TODO: obviously list these instead of taking the first
            var peer = peers.FirstOrDefault();

            if (peer != null)
            {
                var socket = new StreamSocket();
                try
                {
                    // default service name?
                    await socket.ConnectAsync(peer.HostName, ServiceName);

                    var connection = new BluetoothDuplexConnection(socket);

                    if (ConnectionEstablished != null)
                    {
                        ConnectionEstablished(this, connection);
                    }

                    //var writer = new DataWriter(socket.OutputStream);
                    //var reader = new DataReader(socket.InputStream);

                    //var data = "hello";

                    //writer.WriteUInt32((uint)data.Length);
                    //writer.WriteString(data);
                    //await writer.StoreAsync();

                    //writer.Dispose();

                } 
                catch (Exception ex)
                {
                    
                }
            }
        }
    }
}
