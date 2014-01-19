namespace Geomoir.Bluetooth
{
    class ClientConnectedEventArgs
    {
        public BluetoothDevice Device { get; private set; }
        public DuplexConnection Connection { get; private set; }

        public ClientConnectedEventArgs(BluetoothDevice Device, DuplexConnection Connection)
        {
            this.Device = Device;
            this.Connection = Connection;
        }
    }
}
