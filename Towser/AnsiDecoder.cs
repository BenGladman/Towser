using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Text;

namespace Towser
{
    public class AnsiDecoder : TermDecoder
    {
        private readonly ITerminal _terminal;

        public AnsiDecoder(ITerminal terminal)
            : base(null)
        {
            _terminal = terminal;
        }

        private EscapeState _escapeState = EscapeState.Normal;

        private List<DecodedFragment> _fragments = new List<DecodedFragment>();

        private char _c1control = '\0';
        private StringBuilder _c1param = new StringBuilder();
        private char _csicommand;
        private const int _csimax = 17;
        private int[] _csiparams = new int[_csimax];
        private int _csiix = 0;

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

                    ResetState();

                    if (ch >= '@' && ch <= '_') { _c1control = ch; }

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
                            await ExecCommand();
                            break;
                    }
                    return;

                case EscapeState.Csi:
                    // In CSI

                    if (ch >= '@' && ch <= '~')
                    {
                        // End of command
                        _csicommand = ch;
                        await ExecCommand();
                    }
                    else if (ch == ';')
                    {
                        // Next numeric param
                        _csiix += 1;
                        if (_csiix >= _csimax)
                        {
                            // too many numeric params - cancel escape seq
                            _escapeState = EscapeState.Normal;
                        }
                    }
                    else if (ch >= '0' && ch <= '9')
                    {
                        // Add digit to numeric parameter
                        var p = _csiparams[_csiix];
                        p *= 10;
                        p += (byte)(ch - '0');
                        _csiparams[_csiix] = p;
                    }
                    else
                    {
                        // Private mode characters
                        _c1param.Append(ch);
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
                        _c1param.Append(ch);
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

        private void ResetState()
        {
            _c1control = '\0';
            _c1param.Clear();
            _csicommand = '\0';
            Array.Clear(_csiparams, 0, _csiix + 1);
            _csiix = 0;
        }

        private async Task ExecCommand()
        {
            if (_c1control != '\0')
            {
                await AddTextFragment();
                ExecC1();
            }
            _escapeState = EscapeState.Normal;
        }

        private void ExecC1()
        {
            switch (_c1control)
            {
                case '[':
                    ExecCsi();
                    break;

                case ']':
                case '^':
                case '_':
                    // OSC, PM, APC - not implemented
                    //var stringparam = _c1param.ToString();
                    break;
            }
        }

        private void ExecCsi()
        {
            int col, row;

            switch (_csicommand)
            {
                case 'A':
                    // Cursor Up            <ESC>[{COUNT}A
                    row = -(Math.Max(1, _csiparams[0]));
                    _fragments.Add(new DecodedFragment(DecodedFragment.MoveMode.RowRelative, row, 0));
                    break;

                case 'B':
                    // Cursor Down          <ESC>[{COUNT}B
                    row = (Math.Max(1, _csiparams[0]));
                    _fragments.Add(new DecodedFragment(DecodedFragment.MoveMode.RowRelative, row, 0));
                    break;

                case 'C':
                    // Cursor Forward       <ESC>[{COUNT}C
                    col = (Math.Max(1, _csiparams[0]));
                    _fragments.Add(new DecodedFragment(DecodedFragment.MoveMode.ColRelative, 0, col));
                    break;

                case 'D':
                    // Cursor Backward      <ESC>[{COUNT}D
                    col = -(Math.Max(1, _csiparams[0]));
                    _fragments.Add(new DecodedFragment(DecodedFragment.MoveMode.ColRelative, 0, col));
                    break;

                case 'f':
                case 'H':
                    // Cursor Home          <ESC>[{ROW};{COLUMN}H
                    row = Math.Max(0, _csiparams[0] - 1);
                    col = Math.Max(0, _csiparams[1] - 1);
                    _fragments.Add(new DecodedFragment(DecodedFragment.MoveMode.RowAndCol, row, col));
                    break;

                case 'K':
                    // Erase in line
                    _fragments.Add(new DecodedFragment(DecodedFragment.ClearMode.EndOfLine));
                    break;

                case 'J':
                    switch (_csiparams[0])
                    {
                        case 0:
                            _fragments.Add(new DecodedFragment(DecodedFragment.ClearMode.BottomOfScreen));
                            break;
                        case 2:
                            _fragments.Add(new DecodedFragment(DecodedFragment.ClearMode.FullScreen));
                            _fragments.Add(new DecodedFragment(DecodedFragment.MoveMode.RowAndCol, 0, 0));
                            break;
                    }
                    break;

                case 'm':
                    // SGR - Select Graphic Rendition
                    var attrs = new DecodedFragment.Attr[_csiix + 1];
                    for (var i = 0; i <= _csiix; i++)
                    {
                        attrs[i] = (DecodedFragment.Attr)_csiparams[i];
                    }
                    _fragments.Add(new DecodedFragment(attrs));
                    break;

            }
        }

        private async Task AddTextFragment()
        {
            var str = await GetDecodedString();
            if (str.Length > 0) { _fragments.Add(new DecodedFragment(str)); }
        }

        public override async Task Flush()
        {
            await AddTextFragment();
            if (_fragments.Count > 0)
            {
                await _terminal.Write(_fragments);
                _fragments.Clear();
            }
        }

        private enum EscapeState
        {
            Normal,
            Escape,
            Csi,
            String,
            StringEscape,
        }
    }
}