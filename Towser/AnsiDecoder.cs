using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Towser
{
    /// <summary>
    /// Decodes the bytes received from the server into ANSI fragments and sends to the terminal.
    /// </summary>
    public class AnsiDecoder : BaseDecoder
    {
        private readonly IAnsiTerminal _terminal;

        public AnsiDecoder(IAnsiTerminal terminal)
        {
            _terminal = terminal;
        }

        private EscapeState _escapeState = EscapeState.Normal;

        private AnsiEscapeCode _escapeCode;
        private ImmutableList<AnsiFragment> _fragments = ImmutableList<AnsiFragment>.Empty;

        public override async Task AddByte(byte b)
        {
            if ((b == 0x18) || (b == 0x1a))
            {
                // ascii CAN or SUB - cancel escape sequence
                _escapeState = EscapeState.Normal;
                return;
            }

            if (_escapeState == EscapeState.Normal)
            {
                if (b == 0x1b)
                {
                    // escape char
                    _escapeState = EscapeState.Escape;
                    return;
                }
                else if (b >= 0x80 && b <= 0x9f)
                {
                    // c1 control char - equivalent to esc+char
                    _escapeState = EscapeState.Escape;
                    b = (byte)(b - 0x40);
                }
                else
                {
                    await base.AddByte(b);
                    return;
                }
            }

            var ch = (char)b;

            switch (_escapeState)
            {
                case EscapeState.Escape:
                    // Just saw ESC

                    if (ch >= ' ' && ch <= '/')
                    {
                        // escape sequence introducers
                        _escapeCode = new AnsiEscapeCode(ch);
                        _escapeState = EscapeState.Esi;
                    }
                    else if (ch >= '@' && ch <= '_')
                    {
                        _escapeCode = new AnsiEscapeCode(ch);

                        switch (ch)
                        {
                            case 'P':
                                // Device control string
                                _escapeState = EscapeState.String;
                                break;
                            case 'X':
                                // Start of string
                                _escapeState = EscapeState.String;
                                break;
                            case '[':
                                // Control sequence introducer
                                _escapeState = EscapeState.Csi;
                                break;
                            case ']':
                                // Operating system command
                                _escapeState = EscapeState.String;
                                break;
                            case '^':
                                // Privacy message
                                _escapeState = EscapeState.String;
                                break;
                            case '_':
                                // Application program command
                                _escapeState = EscapeState.String;
                                break;
                            default:
                                // 2-character sequence
                                await ExecCommand();
                                break;
                        }
                    }
                    else
                    {
                        // not a valid escape sequence
                        _escapeState = EscapeState.Normal;
                    }
                    return;

                case EscapeState.Esi:
                    if (ch >= '0' && ch <= '~')
                    {
                        // end of sequence
                        _escapeCode = _escapeCode.With(setFinalChar: ch);
                        await ExecCommand();
                    }
                    else
                    {
                        _escapeCode = _escapeCode.With(addIntermediateChar: ch);
                    }
                    break;

                case EscapeState.Csi:
                    // In CSI

                    if (ch >= '@' && ch <= '~')
                    {
                        // End of command
                        _escapeCode = _escapeCode.With(setFinalChar: ch);
                        await ExecCommand();
                    }
                    else if (ch == ';')
                    {
                        // Next numeric param
                        _escapeCode = _escapeCode.With(addCsiParam: ch);
                    }
                    else if (ch >= '0' && ch <= '9')
                    {
                        // Add digit to numeric parameter
                        _escapeCode = _escapeCode.With(addCsiParam: ch);
                    }
                    else
                    {
                        // Private mode characters
                        _escapeCode = _escapeCode.With(addIntermediateChar: ch);

                    }
                    return;

                case EscapeState.String:
                    // In string

                    if (b == 0x07 || b == 0x9c)
                    {
                        // Bell or String Terminator
                        await ExecCommand();
                    }
                    else if (b == 0x1b)
                    {
                        // Escape
                        _escapeState = EscapeState.StringEscape;
                    }
                    else
                    {
                        _escapeCode = _escapeCode.With(addIntermediateChar: ch);
                    }
                    return;

                case EscapeState.StringEscape:
                    if (ch == '\\')
                    {
                        // String Terminator
                        await ExecCommand();
                    }
                    else
                    {
                        _escapeState = EscapeState.String;
                    }
                    return;
            }
        }

        private async Task ExecCommand()
        {
            await AddTextFragment();
            _fragments = _fragments.AddRange(_escapeCode.AnsiFragments());
            _escapeState = EscapeState.Normal;
        }

        private async Task AddTextFragment()
        {
            var str = await GetDecodedString();
            if (str.Length > 0) { _fragments = _fragments.Add(new AnsiFragment(str)); }
        }

        public override async Task Flush()
        {
            await AddTextFragment();
            if (_fragments.Count > 0)
            {
                await _terminal.Write(_fragments);
                _fragments = _fragments.Clear();
            }
        }

        private enum EscapeState
        {
            Normal,
            Escape,
            Esi,
            Csi,
            String,
            StringEscape,
        }
    }
}