using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace CustomActions
{
    public static class CustomActions
    {
        private static readonly string _programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private static readonly string _settingsFolder = Path.Combine(_programData, "Onevinn", "DeploymentScheduler", "Settings");
        private static readonly string _settingsFileName = Path.Combine(_settingsFolder, "Settings.xml");

        [CustomAction]
        public static ActionResult ExtractRootFolder(Session session)
        {
            session.Log("Begin ExtractRootFolder");

            try
            {
                session["MsiPath"] = Path.GetDirectoryName(session["OriginalDatabase"]);
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log($"ExtractRootFolder, Exception: '{ex.Message}'");
            }

            return ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult CopySettingsFile(Session session)
        {
            session.Log("Begin CopySettingsFolder");

            try
            {
                var root = session.CustomActionData["RootPath"];
                var settingsFile = Path.Combine(root, "Settings.xml");

                if (File.Exists(settingsFile))
                {
                    if (!Directory.Exists(_settingsFolder))
                    {
                        Directory.CreateDirectory(_settingsFolder);
                    }

                    File.Copy(settingsFile, _settingsFileName, true);
                }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log($"CopySettingsFile, Exception: '{ex.Message}'");
            }

            return ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult RemoveSettingsAndDB(Session session)
        {
            session.Log("Begin CopySettingsFolder");

            try
            {
                var appDataFolder = Path.Combine(_programData, "Onevinn", "DeploymentScheduler");

                if (Directory.Exists(appDataFolder))
                {
                    Directory.Delete(appDataFolder, true);
                }

                var companyFolder = Path.Combine(_programData, "Onevinn");

                if (Directory.Exists(companyFolder))
                {
                    if (!Directory.EnumerateFileSystemEntries(companyFolder).Any())
                    {
                        Directory.Delete(companyFolder, true);
                    }
                }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log($"RemoveSettingsAndDB, Exception: '{ex.Message}'");
            }

            return ActionResult.Failure;
        }
    }
}
