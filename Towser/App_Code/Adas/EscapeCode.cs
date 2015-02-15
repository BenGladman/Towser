﻿using System.Collections.Immutable;

namespace Towser.Adas
{
    /// <summary>
    /// Represents an ANSI Escape Code.
    /// </summary>
    public struct EscapeCode
    {
        private readonly char _initialChar;
        private readonly ImmutableList<char> _intermediateChars;
        private readonly char _finalChar;
        private readonly ImmutableList<int> _csiParams;
        private readonly int _currentCsiParam;

        public EscapeCode(char initialChar)
            : this()
        {
            _initialChar = initialChar;
        }

        private EscapeCode(EscapeCode source,
            char setInitialChar,
            char addIntermediateChar,
            char setFinalChar,
            char addCsiParam)
        {
            this = source;

            if (setInitialChar != '\0') { this._initialChar = setInitialChar; }

            if (addIntermediateChar != '\0')
            {
                if (_intermediateChars == null) { _intermediateChars = ImmutableList<char>.Empty; }
                _intermediateChars = _intermediateChars.Add(addIntermediateChar);
            }

            if (addCsiParam >= '0' && addCsiParam <= '9')
            {
                _currentCsiParam *= 10;
                _currentCsiParam += (int)(addCsiParam - '0');
            }

            if (setFinalChar != '\0') { this._finalChar = setFinalChar; }

            if (addCsiParam == ';' || setFinalChar != '\0')
            {
                if (_csiParams == null) { _csiParams = ImmutableList<int>.Empty; }
                _csiParams = _csiParams.Add(_currentCsiParam);
                _currentCsiParam = 0;
            }
        }

        private int CsiParam(int index, int defaultValue = 0)
        {
            if (_csiParams == null) { return defaultValue; }
            if (_csiParams.Count <= index) { return defaultValue; }
            return _csiParams[index];
        }

        /// <summary>
        /// Return a new immutable escape code based on this one.
        /// </summary>
        public EscapeCode With(
            char setInitialChar = '\0',
            char addIntermediateChar = '\0',
            char setFinalChar = '\0',
            char addCsiParam = '\0')
        {
            return new EscapeCode(this, setInitialChar, addIntermediateChar, setFinalChar, addCsiParam);
        }

        /// <summary>
        /// Return the fragments from this escape code.
        /// </summary>
        public ImmutableList<Fragment> Fragments()
        {
            var fragments = ImmutableList<Fragment>.Empty;

            switch (_initialChar)
            {
                case 'P':
                    fragments = fragments.Add(new Fragment(Fragment.StringCommand.Dcs, _intermediateChars));
                    break;

                case '[':
                    fragments = fragments.AddRange(CsiFragments());
                    break;

                case ']':
                    fragments = fragments.Add(new Fragment(Fragment.StringCommand.Osc, _intermediateChars));
                    break;

                case '^':
                    fragments = fragments.Add(new Fragment(Fragment.StringCommand.Pm, _intermediateChars));
                    break;

                case '_':
                    fragments = fragments.Add(new Fragment(Fragment.StringCommand.Apc, _intermediateChars));
                    break;
            }

            return fragments;
        }

        /// <summary>
        /// Return the fragments if this is a CSI escape code.
        /// </summary>
        public ImmutableList<Fragment> CsiFragments()
        {
            var fragments = ImmutableList<Fragment>.Empty;

            int col, row;

            switch (_finalChar)
            {
                case 'A':
                    // Cursor Up            <ESC>[{COUNT}A
                    row = -(CsiParam(0, 1));
                    fragments = fragments.Add(new Fragment(Fragment.MoveMode.RowRelative, row, 0));
                    break;

                case 'B':
                    // Cursor Down          <ESC>[{COUNT}B
                    row = (CsiParam(0, 1));
                    fragments = fragments.Add(new Fragment(Fragment.MoveMode.RowRelative, row, 0));
                    break;

                case 'C':
                    // Cursor Forward       <ESC>[{COUNT}C
                    col = (CsiParam(0, 1));
                    fragments = fragments.Add(new Fragment(Fragment.MoveMode.ColRelative, 0, col));
                    break;

                case 'D':
                    // Cursor Backward      <ESC>[{COUNT}D
                    col = -(CsiParam(0, 1));
                    fragments = fragments.Add(new Fragment(Fragment.MoveMode.ColRelative, 0, col));
                    break;

                case 'f':
                case 'H':
                    // Cursor Home          <ESC>[{ROW};{COLUMN}H
                    row = CsiParam(0, 1) - 1;
                    col = CsiParam(1, 1) - 1;
                    fragments = fragments.Add(new Fragment(Fragment.MoveMode.RowAndCol, row, col));
                    break;

                case 'K':
                    // Erase in line
                    fragments = fragments.Add(new Fragment(Fragment.ClearMode.EndOfLine));
                    break;

                case 'J':
                    switch (CsiParam(0))
                    {
                        case 0:
                            fragments = fragments.Add(new Fragment(Fragment.ClearMode.BottomOfScreen));
                            break;
                        case 2:
                            fragments = fragments.Add(new Fragment(Fragment.ClearMode.FullScreen));
                            fragments = fragments.Add(new Fragment(Fragment.MoveMode.RowAndCol, 0, 0));
                            break;
                    }
                    break;

                case 'm':
                    // SGR - Select Graphic Rendition
                    var count = _csiParams.Count;
                    var attrs = new Fragment.Sgr[count];
                    for (var i = 0; i < count; i++)
                    {
                        attrs[i] = (Fragment.Sgr)_csiParams[i];
                    }
                    fragments = fragments.Add(new Fragment(attrs));
                    break;

            }

            return fragments;
        }
    }
}