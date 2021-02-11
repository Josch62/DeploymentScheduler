using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public interface IPropertyItem
    {
        string PropertyName { get; }

        bool BooleanValue { get; set; }

        DateTime DataTimeValue { get; set; }

        int IntegralValue { get; set; }

        long Int64Value { get; set; }

        object ObjectValue { get; set; }

        IResultObject[] ObjectArrayValue { get; set; }

        string StringValue { get; set; }

        string[] StringArrayValue { get; set; }
    }
}
