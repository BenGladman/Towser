using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Towser.Telnet
{
    class Client : IDisposable
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
            NoOption = 0,
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

        private readonly TcpClient _tcpclient = new TcpClient();
        private string _termtype;
        private NetworkStream _stream;

        public StreamWriter StreamWriter { get; private set; }

        public async Task ConnectAsync(string hostname, int port, string termtype, string encodingName)
        {
            if (IsConnected) { return; }
            _termtype = termtype;
            await _tcpclient.ConnectAsync(hostname, port);
            _stream = _tcpclient.GetStream();
            StreamWriter = new StreamWriter(_stream, encodingName);
        }

        public void Dispose()
        {
            if (_stream != null) { _stream.Dispose(); }
            if (_tcpclient != null) { _tcpclient.Close(); }
        }

        /// <summary>
        /// Read up to bufferSize bytes from server.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<byte>> ReadAsync(int bufferSize, CancellationToken ct)
        {
            if (!IsConnected) { return Enumerable.Empty<byte>(); }

            var buffer = new byte[bufferSize];

            var count = 0;
            try
            {
                count = await _stream.ReadAsync(buffer, 0, bufferSize, ct);
            }
            catch (IOException e)
            {
                Debug.WriteLine("Stream read failed {0}", e);
                return Enumerable.Empty<byte>();
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine("Operation cancelled {0}",e);
                return Enumerable.Empty<byte>();
            }

            return ParseTelnet(buffer, count);
        }

        public bool IsConnected
        {
            get { return _tcpclient.Connected && _stream != null; }
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
            var subnegotiation = false;
            var suboption = Options.NoOption;

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
                    var inputverb = (Verbs)getNextByte();

                    switch (inputverb)
                    {
                        case Verbs.IAC:
                            //literal IAC = 255 escaped, so append char 255 to output
                            yield return (byte)Verbs.IAC;
                            break;

                        case Verbs.SB:
                            subnegotiation = true;
                            suboption = (Options)getNextByte();
                            Debug.WriteLine("Negotiate sub request {0} {1}", inputverb.ToString(), suboption.ToString());
                            break;

                        case Verbs.SE:
                            switch (suboption)
                            {
                                case Options.TerminalType:
                                    SendTermtype();
                                    break;
                                default:
                                    Debug.WriteLine("Negotiate sub {0} ignored", suboption.ToString());
                                    break;
                            }
                            suboption = Options.NoOption;
                            subnegotiation = false;
                            break;

                        case Verbs.DO:
                        case Verbs.DONT:
                        case Verbs.WILL:
                        case Verbs.WONT:
                            var inputoption = (Options)getNextByte();

                            Debug.WriteLine("Negotiate request {0} {1}", inputverb.ToString(), inputoption.ToString());

                            Verbs responseverb;

                            var doOrDont = (inputverb == Verbs.DO || inputverb == Verbs.DONT);
                            switch (inputoption)
                            {
                                case Options.Echo:
                                    responseverb = (doOrDont ? Verbs.WONT : Verbs.DO);
                                    break;
                                case Options.SuppressGoAhead:
                                    responseverb = (doOrDont ? Verbs.WILL : Verbs.DO);
                                    break;
                                case Options.TerminalType:
                                    responseverb = (doOrDont ? Verbs.WILL : Verbs.DONT);
                                    break;
                                default:
                                    responseverb = (doOrDont ? Verbs.WONT : Verbs.DONT);
                                    break;
                            }

                            Debug.WriteLine("Negotiate response {0} {1}", responseverb.ToString(), inputoption.ToString());
                            StreamWriter.AddByte((byte)Verbs.IAC);
                            StreamWriter.AddByte((byte)responseverb);
                            StreamWriter.AddByte((byte)inputoption);

                            break;

                        default:
                            Debug.WriteLine("Negotiate ignore {0}", inputverb.ToString(), null);
                            break;
                    }
                    StreamWriter.Flush();
                }
                else if (subnegotiation)
                {
                    // ignore content of subnegotiation
                    Debug.WriteLine("Negotiate sub {0}", input);
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
            StreamWriter.AddByte((byte)Verbs.IAC);
            StreamWriter.AddByte((byte)Verbs.SB);
            StreamWriter.AddByte((byte)Options.TerminalType);
            StreamWriter.AddByte((byte)0);
            foreach (char ch in _termtype)
            {
                StreamWriter.AddByte((byte)ch);
            }
            StreamWriter.AddByte((byte)Verbs.IAC);
            StreamWriter.AddByte((byte)Verbs.SE);
        }
    }
}