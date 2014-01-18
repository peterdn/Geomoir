using Windows.Networking;

namespace Geomoir.Bluetooth
{
    class BluetoothDevice
    {
        public string DisplayName { get; private set; }
        public HostName HostName { get; private set; }
        public string ServiceName { get; private set; }

        public BluetoothDevice(string DisplayName, HostName HostName, string ServiceName)
        {
            this.DisplayName = DisplayName;
            this.HostName = HostName;
            this.ServiceName = ServiceName;
        }
    }
}
