using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Towser
{
    public class Vt100Emulation : BaseEmulation
    {
        private readonly ITerminal _terminal;

        public Vt100Emulation(ITerminal terminal)
            : base(terminal.Write)
        {
            _terminal = terminal;
        }
    }
}