using System;
using System.Text;

namespace Towser
{
    /// <summary>
    /// The BaseEmulation decodes the bytes received from the server into strings and writes the strings to the terminal.
    /// </summary>
    public class BaseEmulation
    {
        private readonly Action<string> _writeToTerminal;

        public BaseEmulation(Action<string> writeToTerminal)
        {
            _writeToTerminal = writeToTerminal;
        }

        private Decoder _standardDecoder;
        private Decoder _altDecoder;
        private Decoder _activeDecoder;

        public void SetEncoding(string encodingName, string altEncodingName)
        {
            var encoding = Encoding.GetEncoding(encodingName);
            _standardDecoder = encoding.GetDecoder();

            var altEncoding = Encoding.GetEncoding(altEncodingName);
            _altDecoder = altEncoding.GetDecoder();
        }

        /// <summary>
        /// Runs after each Flush().
        /// The script function is passed the data from the server (as a decoded string), and returns the string to send to the terminal.
        /// </summary>
        public Func<string, string> ScriptFunc { private get; set; }

        private const int _bufferSize = 1024;

        private byte[] _outBytes = new byte[_bufferSize];

        private int _bytelen = 0;

        private readonly StringBuilder _sb = new StringBuilder();

        private void AppendBytesToSb()
        {
            if (_bytelen > 0)
            {
                if (_activeDecoder == null) { _activeDecoder = _standardDecoder; }

                if (_activeDecoder != null)
                {
                    var chars = new char[_bytelen];
                    var charlen = _activeDecoder.GetChars(_outBytes, 0, _bytelen, chars, 0);
                    if (charlen > 0)
                    {
                        _sb.Append(chars, 0, charlen);
                    }
                }
                _bytelen = 0;
            }
        }

        public virtual void AddByte(byte b)
        {
            if (b == 0x0e)
            {
                // ascii ShiftOut character - use alternate decoder
                AppendBytesToSb();
                _activeDecoder = _altDecoder;
            }
            else if (b == 0x0f)
            {
                // ascii ShiftIn character - use standard decoder
                AppendBytesToSb();
                _activeDecoder = _standardDecoder;
            }
            else
            {
                // append byte to output
                _outBytes[_bytelen] = b;
                _bytelen += 1;
                if (_bytelen > _bufferSize) { Flush(); }
            }
        }

        public void Flush()
        {
            AppendBytesToSb();

            var str = _sb.ToString();
            _sb.Clear();

            if (str.Length > 0 && ScriptFunc != null) { str = ScriptFunc(str); }

            if (str.Length > 0)
            {
                _writeToTerminal(str);
            }
        }
    }
}