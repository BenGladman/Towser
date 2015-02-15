using System.Collections.Generic;
using System.Threading.Tasks;

namespace Towser.Adas
{
    /// <summary>
    /// A terminal that receives lists of <see cref="Fragment"/>s.
    /// </summary>
    public interface ITerminal
    {
        Task Write(IEnumerable<Fragment> fragments);
        Task Error(string s);
    }
}
