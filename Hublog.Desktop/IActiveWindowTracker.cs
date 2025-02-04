using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hublog.Desktop
{
    public interface IActiveWindowTracker
    {
        string GetActiveWindowTitle();
        string GetApplicationIconBase64(string name);
    }
}
