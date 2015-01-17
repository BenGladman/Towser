using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Towser.Aesop
{
    /// <summary>
    /// Represents a decoded ANSI escape code or plain text, which can be serialised to send to the terminal.
    /// </summary>
    public struct Fragment
    {
        [JsonProperty("t", NullValueHandling = NullValueHandling.Ignore)]
        public readonly string Text;

        [JsonProperty("m", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly MoveMode Move;

        [JsonProperty("mc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly sbyte MoveCol;

        [JsonProperty("mr", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly sbyte MoveRow;

        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly ClearMode Clear;

        /// <summary>Select Graphic Rendition</summary>
        [JsonProperty("sgr", NullValueHandling = NullValueHandling.Ignore)]
        public readonly Sgr[] Sgrs;

        /// <summary>Device Control String</summary>
        [JsonProperty("dcs", NullValueHandling = NullValueHandling.Ignore)]
        public readonly String Dcs;

        /// <summary>Operating System Command</summary>
        [JsonProperty("osc", NullValueHandling = NullValueHandling.Ignore)]
        public readonly String Osc;

        /// <summary>Privacy Message</summary>
        [JsonProperty("pm", NullValueHandling = NullValueHandling.Ignore)]
        public readonly String Pm;

        /// <summary>Application Program Control</summary>
        [JsonProperty("apc", NullValueHandling = NullValueHandling.Ignore)]
        public readonly String Apc;

        public Fragment(string str)
            : this()
        {
            Text = str;
        }

        public Fragment(MoveMode m, int row, int col)
            : this()
        {
            Move = m;
            MoveRow = (sbyte)row;
            MoveCol = (sbyte)col;
        }

        public Fragment(ClearMode c)
            : this()
        {
            Clear = c;
        }

        public Fragment(IEnumerable<Sgr> a)
            : this()
        {
            Sgrs = a.ToArray();
        }

        public Fragment(StringCommand s, IEnumerable<char> chars)
            : this()
        {
            var str = String.Concat(chars);
            switch (s)
            {
                case StringCommand.Dcs:
                    Dcs = str;
                    break;
                case StringCommand.Osc:
                    Osc = str;
                    break;
                case StringCommand.Pm:
                    Pm = str;
                    break;
                case StringCommand.Apc:
                    Apc = str;
                    break;
            }
        }

        public enum MoveMode : byte
        {
            NoMove = 0,
            RowAndCol = 1,
            Row = 2,
            RowRelative = 3,
            Col = 4,
            ColRelative = 5,
        }

        public enum ClearMode : byte
        {
            NoClear = 0,
            FullScreen = 1,
            EndOfLine = 2,
            BottomOfScreen = 3,
        }

        /// <summary>
        /// Ansi SGR (Select Graphic Rendition) parameters
        /// </summary>
        public enum Sgr : byte
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

        public enum StringCommand : byte
        {
            No = 0,
            /// <summary>Device Control String</summary>
            Dcs = 1,
            /// <summary>Operating System Command</summary>
            Osc = 2,
            /// <summary>Privacy Message</summary>
            Pm = 3,
            /// <summary>Application Program Control</summary>
            Apc = 4,
        }
    }
}