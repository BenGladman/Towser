using System.Collections.Generic;
using System.Threading.Tasks;

namespace Towser
{
    /// <summary>
    /// A terminal that receives lists of AnsiFragments.
    /// </summary>
    public interface IAnsiTerminal
    {
        Task Write(IEnumerable<AnsiFragment> fragments);
        Task Error(string s);
    }
}
