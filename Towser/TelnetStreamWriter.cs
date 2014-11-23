using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

        private const int _writeBufferSize = 1024;
        private int _writeBufferCount = 0;
        private byte[] _writeBuffer = new byte[_writeBufferSize];

        public void AddByte(byte b)
        {
            lock (_writeBuffer)
            {
                if (_writeBufferCount < _writeBufferSize)
                {
                    _writeBuffer[_writeBufferCount] = b;
                    _writeBufferCount += 1;
                }
                else
                {
                    throw new InvalidOperationException("Write buffer overflowed");
                }
            }
        }

        /// <summary>
        /// Write a single byte to the server.
        /// </summary>
        private async Task WriteAsync(byte b, bool escapeIAC = true)
        {
            const byte IAC = 255;

            AddByte(b);
            if (_writeBufferCount == _writeBufferSize) { await FlushAsync(); }

            // escape literal IAC
            if (escapeIAC && b == IAC)
            {
                AddByte(b);
                if (_writeBufferCount == _writeBufferSize) { await FlushAsync(); }
            }
        }

        /// <summary>
        /// Write bytes to server.
        /// </summary>
        /// <param name="bytes"></param>
        public async Task WriteAsync(IEnumerable<byte> bytes, bool escapeIAC = true)
        {
            foreach (byte b in bytes) { await WriteAsync(b); }
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
            if (_writeBufferCount > 0 && _stream != null && _stream.CanWrite)
            {
                byte[] copyBuffer;

                lock (_writeBuffer)
                {
                    copyBuffer = new byte[_writeBufferCount];
                    Array.Copy(_writeBuffer, copyBuffer, _writeBufferCount);
                    _writeBufferCount = 0;
                }

                return copyBuffer;
            }
            else
            {
                return null;
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