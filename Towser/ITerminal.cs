using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Towser
{
    public interface ITerminal
    {
        Task Write(IEnumerable<AnsiFragment> fragments);
        Task Error(string s);
    }
}
