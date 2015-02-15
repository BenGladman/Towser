using System.Collections.Generic;
using System.Threading.Tasks;

namespace Towser.Adac
{
    /// <summary>
    /// A terminal that receives strings with embedded ANSI escape sequences.
    /// </summary>
    public interface ITerminal
    {
        Task Write(string data);
        Task Error(string s);
    }
}
