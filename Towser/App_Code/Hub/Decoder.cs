using Microsoft.AspNet.SignalR;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Towser.Hub
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
                        case 'P':
                            // Start Device Control String
                            _escapeState = EscapeState.DcsSequence;
                            break;
                        case ']':
                            // Start Operating System Command
                            _escapeState = EscapeState.OscSequence;
                            break;
                        case '^':
                            // Start Privacy Message
                            _escapeState = EscapeState.PmSequence;
                            break;
                        case '_':
                            // Start Application Program Command
                            _escapeState = EscapeState.ApcSequence;
                            break;
                        default:
                            // Send the escape byte and this byte to the client
                            await base.AddByte(0x1b);
                            await base.AddByte(b);
                            _escapeState = EscapeState.Normal;
                            break;
                    }
                    break;

                default:
                    if (_escapeState.HasFlag(EscapeState.Escape))
                    {
                        // Previous byte was escape
                        _escapeState &= ~EscapeState.Escape;
                        b = (byte)((b >= 0x40 && b <= 0x5f) ? (b + 0x40) : 0);
                    }

                    if ((b >= 0x08 && b <= 0x0d) || (b >= 0x20 && b <= 0x7e))
                    {
                        _commandStringBuilder.Append((char)b);
                    }
                    else if (b == 0x1b)
                    {
                        _escapeState |= EscapeState.Escape;
                    }
                    else if (b == 0x9C || b == 0x07)
                    {
                        // Finalise command with String Terminator or Bell.
                        var commandString = _commandStringBuilder.ToString();

                        switch (_escapeState)
                        {
                            case EscapeState.DcsSequence:
                                await _terminal.Dcs(commandString);
                                break;
                            case EscapeState.OscSequence:
                                await _terminal.Osc(commandString);
                                break;
                            case EscapeState.PmSequence:
                                await _terminal.Pm(commandString);
                                break;
                            case EscapeState.ApcSequence:
                                await _terminal.Apc(commandString);
                                break;
                        }
                        _commandStringBuilder.Clear();
                        _escapeState = EscapeState.Normal;
                    }
                    else
                    {
                        await _terminal.Error("Unexpected character in " + _escapeState.ToString());
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

        [Flags]
        private enum EscapeState
        {
            Normal = 0,
            /// <summary>Previous byte was escape.</summary>
            Escape = 1,
            /// <summary>Device Control String.</summary>
            DcsSequence = 1 << 1,
            /// <summary>Operating System Command.</summary>
            OscSequence = 1 << 2,
            /// <summary>Privacy Message.</summary>
            PmSequence = 1 << 3,
            /// <summary>Application Program Command.</summary>
            ApcSequence = 1 << 4,
        }
    }
}