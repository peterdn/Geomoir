using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace Geomoir.Bluetooth
{
    /// <summary>
    /// Represents a duplex connection.
    /// </summary>
    /// <remarks>
    /// Uses a very simple format for variable-size messages. A message consists of a header and body. 
    /// The message header is a UInt32 that specifies the length of the message body in bytes.
    /// The message body follows and must be the specified length in bytes.
    /// </remarks>
    class DuplexConnection
    {
        private readonly DataReader _reader;
        private readonly DataWriter _writer;

        /// <summary>
        /// The string encoding used by the connection.
        /// </summary>
        public UnicodeEncoding StringEncoding
        {
            get
            {
                if (_reader.UnicodeEncoding != _writer.UnicodeEncoding)
                {
                    throw new InvalidOperationException("Mismatched reader and writer UnicodeEncoding properties");
                }
                return _reader.UnicodeEncoding;
            }
            set
            {
                _reader.UnicodeEncoding = value;
                _writer.UnicodeEncoding = value;
            }
        }

        /// <summary>
        /// Creates and initializes an instance of a duplex connection.
        /// </summary>
        /// <param name="Reader">Input DataReader.</param>
        /// <param name="Writer">Output DataWriter.</param>
        public DuplexConnection(DataReader Reader, DataWriter Writer)
        {
            _reader = Reader;
            _writer = Writer;
            this.StringEncoding = UnicodeEncoding.Utf8;
        }

        /// <summary>
        /// Creates and initializes an instance of a duplex connection.
        /// </summary>
        /// <param name="Input">Input stream.</param>
        /// <param name="Output">Output stream.</param>
        public DuplexConnection(IInputStream Input, IOutputStream Output) :
            this(new DataReader(Input), new DataWriter(Output)) { }

        /// <summary>
        /// Creates and initializes an instance of a duplex connection.
        /// </summary>
        /// <param name="Socket">Socket to communicate over.</param>
        public DuplexConnection(StreamSocket Socket) :
            this(Socket.InputStream, Socket.OutputStream) { }


        /// <summary>
        /// Sends a string.
        /// </summary>
        /// <param name="Data">The string to send.</param>
        /// <returns>The asynchronous request.</returns>
        public async Task SendString(string Data)
        {
            // First send the length of the string.
            _writer.WriteUInt32((UInt32) Data.Length);
            _writer.WriteString(Data);

            await _writer.StoreAsync();
        }

        /// <summary>
        /// Receives a string.
        /// </summary>
        /// <returns>The asynchronous request.</returns>
        public async Task<string> ReceiveString()
        {
            var dataLength = await ReceiveMessage();
            return _reader.ReadString(dataLength);
        }

        /// <summary>
        /// Sends a 32-bit unsigned integer.
        /// </summary>
        /// <param name="Value">The value to send.</param>
        /// <returns>The asynchronous request.</returns>
        public async Task SendUInt32(UInt32 Value)
        {
            _writer.WriteUInt32(Value);
            await _writer.StoreAsync();
        }

        /// <summary>
        /// Receives a 32-bit unsigned integer.
        /// </summary>
        /// <returns>The asynchronous request.</returns>
        public async Task<UInt32> ReceiveUInt32()
        {
            var bytesRead = await _reader.LoadAsync(sizeof (UInt32));
            if (bytesRead != sizeof(UInt32)) 
                throw new EndOfStreamException();
            return _reader.ReadUInt32();
        }

        /// <summary>
        /// Recieves a serializable object. 
        /// </summary>
        /// <typeparam name="T">The serializable type.</typeparam>
        /// <returns>The asynchronous request.</returns>
        public async Task<T> ReceiveObject<T>() where T : class
        {
            var serializer = new DataContractSerializer(typeof(T));

            var dataLength = await ReceiveMessage();

            // This is stupidly inefficient, right? We must
            // create a byte[] array of EXACTLY the right size
            // each time. I wonder if it would be better to use
            // a ReadByte() loop and fixed size buffer...

            var buffer = new byte[dataLength];
            _reader.ReadBytes(buffer);

            var memoryStream = new MemoryStream(buffer, 0, (int)dataLength);
            return serializer.ReadObject(memoryStream) as T;
        }

        /// <summary>
        /// Sends a serializable object.
        /// </summary>
        /// <typeparam name="T">The serializable type.</typeparam>
        /// <param name="Data">The object to send.</param>
        /// <returns>The asynchronous request.</returns>
        public async Task SendObject<T>(T Data) where T : class
        {
            var memoryStream = new MemoryStream();

            var serializer = new DataContractSerializer(typeof (T));

            serializer.WriteObject(memoryStream, Data);

            var buffer = memoryStream.GetWindowsRuntimeBuffer();

            _writer.WriteUInt32(buffer.Length);
            _writer.WriteBuffer(buffer);
            await _writer.StoreAsync();
        }

        private async Task<UInt32> ReceiveMessage()
        {
            // First read the length of the string we expect to follow.
            var bytesRead = await _reader.LoadAsync(sizeof(UInt32));

            if (bytesRead != sizeof(UInt32))
                throw new EndOfStreamException();

            var dataLength = _reader.ReadUInt32();

            bytesRead = await _reader.LoadAsync(dataLength);

            if (bytesRead != dataLength)
                throw new EndOfStreamException();

            return bytesRead;
        }
    }
}
