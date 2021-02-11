using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserScheduler.Converters;

namespace UserScheduler.Enums
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum RemindInterval
    {
        [Description("30 minutes")]
        Minutes30,
        [Description("One hour")]
        Hours1,
        [Description("Three hours")]
        Hours3,
        [Description("Six hours")]
        Hours6,
        [Description("Tomorrow")]
        Tomorrow,
        [Description("Never")]
        Never,
    }
}
