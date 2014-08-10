using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Signet
{
    class TelnetClient
    {
        enum Verbs : byte
        {
            WILL = 251,
            WONT = 252,
            DO = 253,
            DONT = 254,
            IAC = 255
        }

        enum Options : byte
        {
            Echo = 1,
            SuppressGoAhead = 3,
            Status = 5,
            TimingMark = 6,
            TerminalType = 24,
            WindowSize = 31,
            TerminalSpeed = 32,
            RemoteFlowControl = 33,
            LineMode = 34,
            EnvironmentVariables = 36
        }

        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly Encoding _encoding;
        private readonly Decoder _decoder;

        public TelnetClient(string hostname, int port, string encodingName)
        {
            _client = new TcpClient(hostname, port);
            _stream = _client.GetStream();
            _encoding = Encoding.GetEncoding(encodingName);
            _decoder = _encoding.GetDecoder();
        }

        public void Write(string data)
        {
            if (!IsConnected) return;

            var bytes = _encoding.GetBytes(data);
            foreach (byte b in bytes)
            {
                _stream.WriteByte(b);
                // escape literal IAC
                if (b == (byte)Verbs.IAC) { _stream.WriteByte(b); }
            }
        }

        public string Read()
        {
            if (!IsConnected) return null;

            const int bufferSize = 1024;
            var buffer = new byte[bufferSize];

            try
            {
                var count = _stream.Read(buffer, 0, bufferSize);
                var str = ParseTelnet(buffer, count);
                Debug.WriteLine("Read from stream {0} bytes {1}", count, str);
                return str;
            }
            catch (IOException e)
            {
                Debug.WriteLine("Stream read failed {0}", e);
                return null;
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                _stream.Close();
                _client.Close();
            }
        }

        public bool IsConnected
        {
            get { return _client.Connected; }
        }

        /// <summary>
        /// Logic mostly from http://www.codeproject.com/Articles/19071/Quick-tool-A-minimalistic-Telnet-library
        /// conceived by Tom Janssens on 2007/06/06  for codeproject
        /// http://www.corebvba.be
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private string ParseTelnet(byte[] buffer, int count)
        {
            var bytes = new byte[count];
            var bytelen = 0;

            for (var ix = 0; ix < count; ix++)
            {
                byte input = buffer[ix];

                if (input == (byte)Verbs.IAC)
                {
                    ix += 1;
                    if (ix >= count) { break; }
                    var inputverb = buffer[ix];

                    switch (inputverb)
                    {
                        case (byte)Verbs.IAC:
                            //literal IAC = 255 escaped, so append char 255 to output
                            bytes[bytelen] = inputverb;
                            bytelen += 1;
                            break;

                        case (byte)Verbs.DO:
                        case (byte)Verbs.DONT:
                        case (byte)Verbs.WILL:
                        case (byte)Verbs.WONT:
                            // reply to all commands with "WONT", unless it is SGA (suppress go ahead) or ECHO
                            ix += 1;
                            if (ix >= count) { break; }
                            var inputoption = buffer[ix];

                            Debug.WriteLine("Negotiate request {0} {1}", ((Verbs)inputverb).ToString(), ((Options)inputoption).ToString());

                            byte responseverb;

                            var doOrDont = (inputverb == (byte)Verbs.DO || inputverb == (byte)Verbs.DONT);
                            switch (inputoption)
                            {
                                case (byte)Options.Echo:
                                    responseverb = (doOrDont ? (byte)Verbs.WONT : (byte)Verbs.DO);
                                    break;
                                case (byte)Options.SuppressGoAhead:
                                    responseverb = (doOrDont ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                    break;
                                default:
                                    responseverb = (doOrDont ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                    break;
                            }

                            Debug.WriteLine("Negotiate response {0} {1}", ((Verbs)responseverb).ToString(), ((Options)inputoption).ToString());
                            _stream.WriteByte((byte)Verbs.IAC);
                            _stream.WriteByte(responseverb);
                            _stream.WriteByte((byte)inputoption);

                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    // append byte to output
                    bytes[bytelen] = input;
                    bytelen += 1;
                }
            }

            if (bytelen > 0)
            {
                var chars = new char[bytelen];
                var charlen = _decoder.GetChars(bytes, 0, bytelen, chars, 0);
                if (charlen > 0)
                {
                    return new String(chars, 0, charlen);
                }
            }

            return null;
        }
    }
}