using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Towser
{
    public struct DecodedFragment
    {
        [JsonProperty("t", NullValueHandling = NullValueHandling.Ignore)]
        public readonly string Text;

        [JsonProperty("m", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly MoveMode MoveModeP;

        [JsonProperty("mc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly sbyte MoveCol;

        [JsonProperty("mr", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly sbyte MoveRow;

        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly ClearMode ClearModeP;

        [JsonProperty("a", NullValueHandling = NullValueHandling.Ignore)]
        public readonly Attr[] AttrsP;

        public DecodedFragment(string str) : this()
        {
            Text = str;
        }

        public DecodedFragment(MoveMode m, int row, int col) : this()
        {
            MoveModeP = m;
            MoveRow = (sbyte)row;
            MoveCol = (sbyte)col;
        }

        public DecodedFragment(ClearMode c) : this()
        {
            ClearModeP = c;
        }

        public DecodedFragment(Attr[] a) : this()
        {
            AttrsP = a;
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
        public enum Attr : byte
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
}