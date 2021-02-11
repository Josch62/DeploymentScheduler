using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationEditor.Sccm
{
    public class CMColl
    {
        public string Name { get; set; }

        public string CollectionId { get; set; }

        public int CollectionVariablesCount { get; set; } = 0;
    }
}
