using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Towser
{
    public interface ITerminal
    {
        void Write(string s);
        void Error(string s);
    }
}
