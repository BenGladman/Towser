using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Towser
{
    class TelnetClient
    {
        enum Verbs : byte
        {
            SE = 240,
            SB = 250,
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
        private readonly Decoder _standardDecoder;
        private readonly Decoder _altDecoder;
        private readonly string _termtype;
        private Decoder _activeDecoder;

        public TelnetClient(string hostname, int port, string encodingName, string altEncodingName, string termtype)
        {
            _client = new TcpClient(hostname, port);
            _stream = _client.GetStream();
            _encoding = Encoding.GetEncoding(encodingName);
            _standardDecoder = _encoding.GetDecoder();
            _altDecoder = Encoding.GetEncoding(altEncodingName).GetDecoder();
            _activeDecoder = _standardDecoder;
            _termtype = termtype;
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
            bool subnegotiation = false;
            var ix = 0;

            var sb = new StringBuilder();

            Func<byte> getNextByte = delegate()
            {
                byte r = (ix < count) ? buffer[ix] : (byte)0;
                ix += 1;
                return r;
            };

            Action appendBytesToSb = delegate()
            {
                if (bytelen > 0)
                {
                    var chars = new char[bytelen];
                    var charlen = _activeDecoder.GetChars(bytes, 0, bytelen, chars, 0);
                    if (charlen > 0)
                    {
                        sb.Append(chars, 0, charlen);
                    }
                    bytelen = 0;
                }
            };

            while (ix < count)
            {
                var input = getNextByte();

                if (input == (byte)Verbs.IAC)
                {
                    var inputverb = getNextByte();

                    switch ((Verbs)inputverb)
                    {
                        case Verbs.IAC:
                            //literal IAC = 255 escaped, so append char 255 to output
                            bytes[bytelen] = inputverb;
                            bytelen += 1;
                            break;

                        case Verbs.SB:
                            subnegotiation = true;
                            var suboption = getNextByte();
                            Debug.WriteLine("Negotiate sub request {0} {1}", ((Verbs)inputverb).ToString(), ((Options)suboption).ToString());
                            break;

                        case Verbs.SE:
                            subnegotiation = false;
                            break;

                        case Verbs.DO:
                        case Verbs.DONT:
                        case Verbs.WILL:
                        case Verbs.WONT:
                            var inputoption = getNextByte();

                            Debug.WriteLine("Negotiate request {0} {1}", ((Verbs)inputverb).ToString(), ((Options)inputoption).ToString());

                            byte responseverb;

                            var doOrDont = (inputverb == (byte)Verbs.DO || inputverb == (byte)Verbs.DONT);
                            switch ((Options)inputoption)
                            {
                                case Options.Echo:
                                    responseverb = (doOrDont ? (byte)Verbs.WONT : (byte)Verbs.DO);
                                    break;
                                case Options.SuppressGoAhead:
                                    responseverb = (doOrDont ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                    break;
                                case Options.TerminalType:
                                    responseverb = (doOrDont ? (byte)Verbs.WILL : (byte)Verbs.DONT);
                                    break;
                                default:
                                    responseverb = (doOrDont ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                    break;
                            }

                            Debug.WriteLine("Negotiate response {0} {1}", ((Verbs)responseverb).ToString(), ((Options)inputoption).ToString());
                            _stream.WriteByte((byte)Verbs.IAC);
                            _stream.WriteByte(responseverb);
                            _stream.WriteByte((byte)inputoption);

                            if (inputoption == (byte)Options.TerminalType && responseverb == (byte)Verbs.WILL)
                            {
                                SendTermtype();
                            }

                            break;

                        default:
                            Debug.WriteLine("Negotiate ignore {0}", ((Verbs)inputverb).ToString(), null);
                            break;
                    }
                }
                else if (subnegotiation)
                {
                    // ignore content of subnegotiation
                    Debug.WriteLine("Negotiate sub ignore {0}", input);
                }
                else if (input == 0x0e)
                {
                    // ascii ShiftOut character - use alternate decoder
                    appendBytesToSb();
                    _activeDecoder = _altDecoder;
                }
                else if (input == 0x0f)
                {
                    // ascii ShiftIn character - use standard decoder
                    appendBytesToSb();
                    _activeDecoder = _standardDecoder;
                }
                else
                {
                    // append byte to output
                    bytes[bytelen] = input;
                    bytelen += 1;
                }
            }

            appendBytesToSb();

            if (sb.Length > 0)
            {
                return sb.ToString();
            }
            else
            {
                return null;
            }
        }

        private void SendTermtype()
        {
            Debug.WriteLine("Negotiate send termtype {0}", _termtype, null);
            _stream.WriteByte((byte)Verbs.IAC);
            _stream.WriteByte((byte)Verbs.SB);
            _stream.WriteByte((byte)Options.TerminalType);
            _stream.WriteByte((byte)0);
            foreach (char ch in _termtype)
            {
                _stream.WriteByte((byte)ch);
            }
            _stream.WriteByte((byte)Verbs.IAC);
            _stream.WriteByte((byte)Verbs.SE);
        }

        /// <summary>
        /// Continuously read from telnet until disconnected
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="client"></param>
        public void ReadLoop(Action<string> onRead, Action onDisconnect, string loginPrompt = null, string login = null, string passwordPrompt = null, string password = null)
        {
            var loginAuto = (!String.IsNullOrEmpty(loginPrompt) && !String.IsNullOrEmpty(login));
            var passwordAuto = (!String.IsNullOrEmpty(passwordPrompt) && !String.IsNullOrEmpty(password));

            while (IsConnected)
            {
                var str = Read();

                if (String.IsNullOrEmpty(str)) { continue; }

                if (loginAuto && str.EndsWith(loginPrompt, StringComparison.Ordinal))
                {
                    Write(login + "\r\n");
                    loginAuto = false;
                    str = str.Remove(str.Length - loginPrompt.Length);
                }

                if (passwordAuto && str.EndsWith(passwordPrompt, StringComparison.Ordinal))
                {
                    Write(password + "\r\n");
                    passwordAuto = false;
                    str = str.Remove(str.Length - passwordPrompt.Length);
                }

                if (str.Length > 0)
                {
                    onRead(str);
                }
            }

            if (onDisconnect != null) { onDisconnect(); }
        }
    }
}