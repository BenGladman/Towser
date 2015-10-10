using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Towser.Telnet
{
    public class StreamWriter
    {
        private readonly NetworkStream _stream;
        private readonly Encoding _encoding;
        private readonly List<byte> _writeBuffer;

        public StreamWriter(NetworkStream stream, string encodingName)
        {
            _stream = stream;
            _encoding = Encoding.GetEncoding(encodingName);
            _writeBuffer = new List<byte>();
        }

        public void AddByte(byte b)
        {
            lock (_writeBuffer)
            {
                _writeBuffer.Add(b);
            }
        }

        /// <summary>
        /// Write bytes to server.
        /// </summary>
        /// <param name="bytes"></param>
        public async Task WriteAsync(IEnumerable<byte> bytes, bool escapeIAC = true)
        {
            const byte IAC = 255;
            foreach (byte b in bytes)
            {
                AddByte(b);
                // escape literal IAC
                if (escapeIAC && b == IAC) { AddByte(b); }
            }
            await FlushAsync();
        }

        /// <summary>
        /// Write string to server.
        /// </summary>
        /// <param name="data"></param>
        public async Task WriteAsync(string data)
        {
            var bytes = _encoding.GetBytes(data);
            await WriteAsync(bytes);
        }

        private byte[] GetBytesToFlush()
        {
            byte[] r = null;

            if (_writeBuffer.Count > 0)
            {
                lock (_writeBuffer)
                {
                    r = _writeBuffer.ToArray();
                    _writeBuffer.Clear();
                }
            }

            return r;
        }

        public void Flush()
        {
            var bytes = GetBytesToFlush();
            if (bytes != null) { _stream.Write(bytes, 0, bytes.Length); }
        }

        public async Task FlushAsync()
        {
            var bytes = GetBytesToFlush();
            if (bytes != null) { await _stream.WriteAsync(bytes, 0, bytes.Length); }
        }
    }
}