using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace Geomoir.Bluetooth
{
    class BluetoothDuplexConnection
    {
        private DataReader _reader;
        private DataWriter _writer;

        public UnicodeEncoding StringEncoding { get; set; }

        public BluetoothDuplexConnection(DataReader Reader, DataWriter Writer)
        {
            _reader = Reader;
            _writer = Writer;
            this.StringEncoding = UnicodeEncoding.Utf8;
        }

        public BluetoothDuplexConnection(StreamSocket Socket)
        {
            _reader = new DataReader(Socket.InputStream);
            _writer = new DataWriter(Socket.OutputStream);
            this.StringEncoding = UnicodeEncoding.Utf8;
        }

        public async Task SendString(string Data)
        {
            // TODO ensure connected
            //AssertConnected();

            _writer.UnicodeEncoding = StringEncoding;

            // First send the length of the string.
            _writer.WriteUInt32((UInt32)Data.Length);
            _writer.WriteString(Data);

            await _writer.StoreAsync();
        }

        public async Task<string> ReceiveString()
        {
            //AssertConnected();

            _reader.UnicodeEncoding = StringEncoding;

            // First read the length of the string we expect to follow.
            var bytesRead = await _reader.LoadAsync(sizeof(UInt32));

            if (bytesRead != sizeof(UInt32))
                throw new EndOfStreamException();

            var dataLength = _reader.ReadUInt32();

            bytesRead = await _reader.LoadAsync(dataLength);

            if (bytesRead != dataLength)
                throw new EndOfStreamException();

            return _reader.ReadString(dataLength);
        }
    }
}
