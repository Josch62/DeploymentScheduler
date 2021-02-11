using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using SchedulerCommon.Logging;
using SchedulerSettings.Models;

namespace SchedulerCommon.RegistryMethods
{
    public static class Reg
    {
        private static readonly ServiceEventSource _log = ServiceEventSource.Log;
        private static readonly string _regPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";

        public static void SetLegalNotice(LegalNotice legalNotice)
        {
            try
            {
                var pRegHive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var pRootKey = pRegHive.OpenSubKey(_regPath, true);
                pRootKey.SetValue("legalnoticecaption", legalNotice.LegalNoticeCaption, RegistryValueKind.String);
                pRootKey.SetValue("legalnoticetext", legalNotice.LegalNoticeText, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void RemoveLegalNotice()
        {
            try
            {
                var pRegHive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var pRootKey = pRegHive.OpenSubKey(_regPath, true);
                pRootKey.SetValue("legalnoticecaption", string.Empty, RegistryValueKind.String);
                pRootKey.SetValue("legalnoticetext", string.Empty, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }
    }
}
