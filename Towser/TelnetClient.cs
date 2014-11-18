using System;
using System.Collections.Generic;
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
        private readonly string _termtype;
        private readonly Encoding _encoding;

        public TelnetClient(string hostname, int port, string termtype, string encodingName)
        {
            _client = new TcpClient(hostname, port);
            _stream = _client.GetStream();
            _termtype = termtype;
            _encoding = Encoding.GetEncoding(encodingName);
        }

        /// <summary>
        /// Write bytes to server.
        /// </summary>
        /// <param name="bytes"></param>
        public void Write(IEnumerable<byte> bytes)
        {
            if (!IsConnected) return;

            foreach (byte b in bytes)
            {
                _stream.WriteByte(b);
                // escape literal IAC
                if (b == (byte)Verbs.IAC) { _stream.WriteByte(b); }
            }
        }

        /// <summary>
        /// Write string to server.
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data)
        {
            var bytes = _encoding.GetBytes(data);
            Write(bytes);
        }

        /// <summary>
        /// Read up to bufferSize bytes from server. Blocking read.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<byte> Read(int bufferSize)
        {
            if (!IsConnected) { yield break; }

            var buffer = new byte[bufferSize];

            var count = 0;
            try
            {
                count = _stream.Read(buffer, 0, bufferSize);
            }
            catch (IOException e)
            {
                Debug.WriteLine("Stream read failed {0}", e);
                yield break;
            }

            foreach (var b in ParseTelnet(buffer, count)) { yield return b; }
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
        private IEnumerable<byte> ParseTelnet(byte[] buffer, int count)
        {
            bool subnegotiation = false;
            var ix = 0;

            Func<byte> getNextByte = delegate()
            {
                byte r = (ix < count) ? buffer[ix] : (byte)0;
                ix += 1;
                return r;
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
                            yield return inputverb;
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
                else
                {
                    yield return input;
                }
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
    }
}