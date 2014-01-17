using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Geomoir_Tracker
{
    class BluetoothClient
    {
        public static readonly Guid ServiceGUID = Guid.Parse("EEB35C6F-33DB-4DE3-8B4E-CFA313E92640");

        public async Task Connect()
        {
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var peers = await PeerFinder.FindAllPeersAsync();

            // TODO: obviously list these instead of taking the first
            var peer = peers.FirstOrDefault();

            if (peer != null)
            {
                var socket = new StreamSocket();
                try
                {
                    // default service name?
                    await socket.ConnectAsync(peer.HostName, "1");
                    var writer = new DataWriter(socket.OutputStream);

                    var data = "hello";
                    
                    writer.WriteUInt32((uint)data.Length);
                    writer.WriteString(data);
                    await writer.StoreAsync();

                    writer.Dispose();
                } 
                catch (Exception ex)
                {
                    
                }
            }
        }
    }
}
