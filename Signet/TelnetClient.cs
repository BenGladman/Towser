using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Signet
{
    class TelnetClient
    {
        enum Verbs
        {
            WILL = 251,
            WONT = 252,
            DO = 253,
            DONT = 254,
            IAC = 255
        }

        enum Options
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

        TcpClient _client;
        NetworkStream _stream;

        public TelnetClient(string hostname, int port)
        {
            _client = new TcpClient(hostname, port);
            _stream = _client.GetStream();
        }

        public void Write(string data)
        {
            if (!IsConnected) return;

            foreach (char ch in data)
            {
                byte b = (byte)ch;
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
            var sb = new StringBuilder();

            for (var ix = 0; ix < count; ix++)
            {
                byte input = buffer[ix];

                if (input == (byte)Verbs.IAC)
                {
                    ix += 1;
                    var inputverb = (ix < count) ? buffer[ix] : -1;

                    switch (inputverb)
                    {
                        case -1:
                            break;

                        case (byte)Verbs.IAC:
                            //literal IAC = 255 escaped, so append char 255 to string
                            sb.Append((char)inputverb);
                            break;

                        case (byte)Verbs.DO:
                        case (byte)Verbs.DONT:
                        case (byte)Verbs.WILL:
                        case (byte)Verbs.WONT:
                            // reply to all commands with "WONT", unless it is SGA (suppress go ahead) or ECHO
                            ix += 1;
                            var inputoption = (count >= 3) ? buffer[2] : -1;
                            if (inputoption == -1) { break; }

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
                    sb.Append((char)input);
                }
            }

            return sb.ToString();
        }
    }
}