using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public interface IResultObject : IDisposable
    {
        Dictionary<string, IPropertyItem> Properties { get; }

        IPropertyItem this[string name] { get; }

        IResultObject ExecuteMethod(string methodName, Dictionary<string, object> methodParameters);
    }
}
