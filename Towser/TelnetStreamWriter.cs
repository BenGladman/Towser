using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Towser
{
    public class TelnetStreamWriter
    {
        private readonly NetworkStream _stream;
        private readonly Encoding _encoding;

        public TelnetStreamWriter(NetworkStream stream, string encodingName)
        {
            _stream = stream;
            _encoding = Encoding.GetEncoding(encodingName);
        }

        private ImmutableList<byte> _writeBuffer = ImmutableList.Create<byte>();

        public void AddByte(byte b)
        {
            _writeBuffer = _writeBuffer.Add(b);
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
            var wb = _writeBuffer;
            _writeBuffer = _writeBuffer.Clear();

            if (wb.Count == 0)
            {
                return null;
            }
            else
            {
                return wb.ToArray();
            }
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