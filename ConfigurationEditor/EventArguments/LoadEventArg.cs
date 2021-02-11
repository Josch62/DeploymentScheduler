using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationEditor.EventArguments
{
    public class LoadEventArg : EventArgs
    {
        public string Settings { get; set; }
    }
}
