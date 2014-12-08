using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Towser
{
    public interface ITerminal
    {
        Task Write(string s);
        Task Error(string s);
        Task Clear(TerminalClearType type);
        Task Move(int row, int col);
        Task MoveRow(int row, bool relativeRow);
        Task MoveCol(int col, bool relativeCol);
        Task Attr(TerminalAttributes attr);
        Task Attrs(TerminalAttributes[] attrs);
    }

    public enum TerminalClearType
    {
        FullScreen = 0,
        EndOfLine = 1,
        BottomOfScreen = 2,
    }

    /// <summary>
    /// Ansi SGR (Select Graphic Rendition) parameters
    /// </summary>
    public enum TerminalAttributes
    {
        Reset = 0,
        Bold = 1,
        Faint = 2,
        Italic = 3,
        Underline = 4,
        Blink = 5,
        Reverse = 7,
        Strikethrough = 9,
        BoldOff = 21,
        BoldFaintOff = 22,
        ItalicOff = 23,
        UnderlineOff = 24,
        BlinkOff = 25,
        ReverseOff = 27,
        StrikethroughOff = 29,
        FgBlack = 30,
        FgRed = 31,
        FgGreen = 32,
        FgYellow = 33,
        FgBlue = 34,
        FgMagenta = 35,
        FgCyan = 36,
        FgWhite = 37,
        FgDefault = 39,
        BgBlack = 40,
        BgRed = 41,
        BgGreen = 42,
        BgYellow = 43,
        BgBlue = 44,
        BgMagenta = 45,
        BgCyan = 46,
        BgWhite = 47,
        BgDefault = 49,
    }
}
