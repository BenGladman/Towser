using Microsoft.AspNet.SignalR;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Towser.Adac
{
    /// <summary>
    /// Decodes the bytes received from the server into strings and writes the strings to the terminal.
    /// </summary>
    public class Decoder : BaseDecoder
    {
        private readonly ITerminal _terminal;

        public Decoder(ITerminal terminal)
        {
            _terminal = terminal;
        }

        private EscapeState _escapeState = EscapeState.Normal;
        private readonly StringBuilder _commandStringBuilder = new StringBuilder();

        public override async Task AddByte(byte b)
        {
            switch (_escapeState)
            {
                case EscapeState.Normal:
                    if (b == 0x1b)
                    {
                        _escapeState = EscapeState.Escape;
                        break;
                    }
                    else
                    {
                        await base.AddByte(b);
                    }
                    break;

                case EscapeState.Escape:
                    switch ((char)b)
                    {
                        case ']':
                            _escapeState = EscapeState.Osc;
                            break;
                        case '^':
                            _escapeState = EscapeState.Pm;
                            break;
                        case '_':
                            _escapeState = EscapeState.Apc;
                            break;
                        default:
                            // send the escape byte and this byte to the client
                            await base.AddByte(0x1b);
                            await base.AddByte(b);
                            _escapeState = EscapeState.Normal;
                            break;
                    }
                    break;

                default:
                    if ((b >= 0x08 && b <= 0x0d) || (b >= 0x20 && b <= 0x7e))
                    {
                        _commandStringBuilder.Append((char)b);
                    }
                    else
                    {
                        var commandString = _commandStringBuilder.ToString();

                        switch (_escapeState)
                        {
                            case EscapeState.Osc:
                                await _terminal.Osc(commandString);
                                break;
                            case EscapeState.Pm:
                                await _terminal.Pm(commandString);
                                break;
                            case EscapeState.Apc:
                                await _terminal.Apc(commandString);
                                break;
                        }
                        _commandStringBuilder.Clear();
                        _escapeState = EscapeState.Normal;
                    }
                    break;
            }
        }

        public override async Task Flush()
        {
            var str = await GetDecodedString();
            if (str.Length > 0)
            {
                await _terminal.Write(str);
            }
        }

        private enum EscapeState
        {
            Normal,
            Escape,
            Osc,
            Pm,
            Apc,
        }
    }
}