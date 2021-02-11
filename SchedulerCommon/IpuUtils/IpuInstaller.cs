using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using SchedulerCommon.NativeMethods;
using SchedulerCommon.Wmi;
using SchedulerSettings;

namespace SchedulerCommon.IpuUtils
{
    public static class IpuInstaller
    {
        private static readonly string _cmd1 = "/Update /PreDownload /quiet /noreboot";
        private static readonly string _cmd2 = "/Update /Install /quiet /noreboot";
        private static readonly string _cmd3 = "/Update /Finalize /quiet /noreboot";

        private static readonly string _systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        private static readonly string _iniFilePath = $"{_systemDrive}Users\\Default\\AppData\\Local\\Microsoft\\Windows\\WSUS";
        private static readonly string _progressApp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IpuProgress.exe");

        public static bool _Install(string contentPath)
        {
            var updateBox = Path.Combine(contentPath, "WindowsUpdateBox.exe");

            if (!File.Exists(updateBox))
            {
                Globals.Log.Error($"Attempt to run Ipu failed, content '{updateBox}' missing.");
                return false;
            }

            var driverStatus = false;

            if (SettingsUtils.Settings.IpuApplication.UseWimDrivers)
            {
                driverStatus = ExtractDrivers();
            }

            try
            {
                CreateConfFiles(contentPath, driverStatus);
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
                return false;
            }

            RegistryMethods.SetProgressStatus(1);

            if (SettingsUtils.Settings.IpuApplication.ShowProgress)
            {
                UnsafeNativeMethods.Run(_progressApp, string.Empty, false);
            }

            Globals.Log.Information("Ipu: Running command 1.");
            var result = RunCmd(updateBox, _cmd1);

            if (result != 0)
            {
                RunSetupDiag(contentPath);
                RegistryMethods.MoveSetupDiag(result);
                Globals.Log.Error($"Result of Ipu command 1 = '0x{result:X8}', aborting.");
                return false;
            }
            else
            {
                Globals.Log.Information($"Ipu: command returned: '0x{result:X8}'");
            }

            if (Directory.Exists(Path.Combine(contentPath, "Patches")))
            {
                Firewall.BlockSetupHost();
            }

            RegistryMethods.SetProgressStatus(2);
            Globals.Log.Information("Ipu: Running command 2.");
            result = RunCmd(updateBox, _cmd2);

            if (result != 0)
            {
                Globals.Log.Error($"Result of Ipu command 2 = '0x{result:X8}', aborting.");
                return false;
            }
            else
            {
                Globals.Log.Information($"Ipu: command returned: '0x{result:X8}'");
            }

            RegistryMethods.SetProgressStatus(3);
            Patch(contentPath);

            Globals.Log.Information("Ipu: Running command 3.");
            RegistryMethods.SetProgressStatus(4);
            result = RunCmd(updateBox, _cmd3);

            if (result != 0)
            {
                Globals.Log.Error($"Result of Ipu command 3 = '0x{result:X8}', aborting.");
                return false;
            }
            else
            {
                Globals.Log.Information($"Ipu: command returned: '0x{result:X8}'");
            }

            return true;
        }

        private static int RunCmd(string exe, string arg)
        {
            var enc = Encoding.UTF8;

            var si = new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = arg,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardErrorEncoding = enc,
                StandardOutputEncoding = enc,
            };

            Process proc = null;

            var bytes = new List<char>();

            try
            {
                proc = Process.Start(si);
                proc.EnableRaisingEvents = true;

                var readout = proc.StandardOutput;
                var cmdResult = readout.ReadToEnd();
                var readerror = proc.StandardError;
                var cmdError = readerror.ReadToEnd();
                proc.WaitForExit();

                cmdResult = cmdResult.Replace("\0", " ");
                cmdError = cmdError.Replace("\0", " ");

                if (!string.IsNullOrEmpty(cmdResult.Trim()))
                {
                    Globals.Log.Information(cmdResult);
                }
                else if (!string.IsNullOrEmpty(cmdError.Trim()))
                {
                    Globals.Log.Error(cmdError);
                    return 1;
                }
            }
            catch
            {
                return 9999;
            }

            return proc.ExitCode;
        }

        private static void Patch(string contentPath)
        {
            var patchPath = Path.Combine(contentPath, "Patches");
            RunCmd("dism.exe", $"/Image:C:\\$WINDOWS.~BT\\NewOS /LogPath:{Path.GetTempPath()}IPUPatchResult.log /Add-Package /PackagePath:\"{patchPath}\"");
        }

        private static void CreateConfFiles(string contentPath, bool hasDrivers)
        {
            var setupIni = "[Setupconfig]" + Environment.NewLine;
            setupIni += "Compat=IgnoreWarning" + Environment.NewLine;
            setupIni += "Priority=Normal" + Environment.NewLine;
            setupIni += "DynamicUpdate=Enable" + Environment.NewLine;
            setupIni += "ResizeRecoveryPartition=Enable" + Environment.NewLine;
            setupIni += $"PostOOBE={contentPath}\\SetupComplete.cmd" + Environment.NewLine;
            setupIni += $"PostRollBack={contentPath}\\ErrorHandler.cmd" + Environment.NewLine;

            if (hasDrivers)
            {
                var driverDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "~IPUDrivers");
                setupIni += $"InstallDrivers={driverDir}" + Environment.NewLine;
            }
            else if (!SettingsUtils.Settings.IpuApplication.UseWimDrivers && !string.IsNullOrEmpty(SettingsUtils.Settings.IpuApplication.CustomDriversFolder))
            {
                var customDir = Environment.ExpandEnvironmentVariables(SettingsUtils.Settings.IpuApplication.CustomDriversFolder);

                if (Directory.Exists(customDir))
                {
                    setupIni += $"InstallDrivers={customDir}" + Environment.NewLine;
                    Globals.Log.Information($"Added 'InstallDrivers={customDir}' to INI file.");
                }
                else
                {
                    Globals.Log.Warning($"Custom drivers folder '{customDir}' missing on disk - skipping drivers.");
                }
            }

            var reflectDriversFolder = Path.Combine(contentPath, "ReflectDrivers");

            if (Directory.Exists(reflectDriversFolder))
            {
                setupIni += $"ReflectDrivers={reflectDriversFolder}" + Environment.NewLine;
            }

            setupIni += "PostRollBackContext=System";

            if (!Directory.Exists(_iniFilePath))
            {
                Directory.CreateDirectory(_iniFilePath);
            }

            var setupIniFile = Path.Combine(_iniFilePath, "Setupconfig.ini");
            File.WriteAllText(setupIniFile, setupIni, Encoding.ASCII);

            var setupComplete = "@ECHO OFF" + Environment.NewLine;
            setupComplete += $"{contentPath}\\SetupComplete\\RunSilent.exe" + Environment.NewLine;
            var setupCompleteFile = Path.Combine(contentPath, "SetupComplete.cmd");
            File.WriteAllText(setupCompleteFile, setupComplete, Encoding.ASCII);

            var errorHandler = "@ECHO OFF" + Environment.NewLine;
            errorHandler += $"{contentPath}\\ErrorHandler\\RunSilent.exe" + Environment.NewLine;
            var errorHandlerFile = Path.Combine(contentPath, "ErrorHandler.cmd");
            File.WriteAllText(errorHandlerFile, errorHandler, Encoding.ASCII);
        }

        private static bool ExtractDrivers()
        {
            try
            {
                var cmpModel = Cimv2.GetComputerMakeModel().Model;
                var exclusions = SettingsUtils.Settings.IpuApplication.ExcludedModels.Split(';');

                if (exclusions.Contains(cmpModel, StringComparer.InvariantCultureIgnoreCase))
                {
                    Globals.Log.Warning($"Computer model '{cmpModel}' is on driver exclusions list - skipping drivers.");
                    return false;
                }

                var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                var ccmcache = Path.Combine(windowsPath, "ccmcache");
                var fileList = new List<FileInfo>();

                foreach (var filename in Directory.GetFiles(ccmcache, "IPUDrivers*.wim", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(filename);
                    fileList.Add(fileInfo);
                }

                if (fileList.Count == 0)
                {
                    Globals.Log.Error($"Failed to locate 'IPUDrivers?.wim'");
                    return false;
                }

                var newestWim = fileList.OrderBy(x => x.LastWriteTime).Last();

                var driverDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "~IPUDrivers");

                Directory.CreateDirectory(driverDir);

                var dismResult = RunCmd("dism.exe", $"/apply-image /imagefile:\"{newestWim.FullName}\" /index:1 /ApplyDir:\"{driverDir}\"");

                if (dismResult != 0)
                {
                    Globals.Log.Error($"Dism failed to extrace drivers, result code: '{dismResult}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
                return false;
            }

            return true;
        }

        private static void RunSetupDiag(string contentPath)
        {
            var setupDiagPath = Path.Combine(contentPath, "SetupDiag");

            try
            {
                if (Directory.Exists(setupDiagPath))
                {
                    var setupDiagFile = Path.Combine(setupDiagPath, "SetupDiag.exe");
                    var setupDiagLogFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs", "SetupDiag");
                    Directory.CreateDirectory(setupDiagLogFolder);
                    var setupDiagXml = Path.Combine(setupDiagLogFolder, "SetupDiagResults.xml");
                    RunCmd(setupDiagFile, $"/ZipLogs:False /Format:xml /Output:{setupDiagXml} /RegPath:HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\SetupDiag\\Results");
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }
    }
}
