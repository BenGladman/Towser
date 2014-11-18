using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

namespace Towser
{
    public class BasicEmulation : ITerminalEmulation
    {
        private readonly ITerminal _terminal;
        private readonly Decoder _standardDecoder;
        private readonly Decoder _altDecoder;
        private Decoder _activeDecoder;

        public BasicEmulation(ITerminal terminal, string encodingName, string altEncodingName)
        {
            _terminal = terminal;

            var encoding = Encoding.GetEncoding(encodingName);
            _standardDecoder = encoding.GetDecoder();

            var altEncoding = Encoding.GetEncoding(altEncodingName);
            _altDecoder = altEncoding.GetDecoder();
        }

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

        public void AddByte(byte b)
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
                _terminal.Write(str);
            }
        }
    }
}