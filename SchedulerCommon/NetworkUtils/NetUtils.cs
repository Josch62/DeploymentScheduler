using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.NetworkUtils
{
    public static class NetUtils
    {
        public static bool HasInternet()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://download.windowsupdate.com"))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
