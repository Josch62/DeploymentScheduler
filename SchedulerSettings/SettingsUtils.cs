using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SchedulerSettings.Models;
using SchedulerSettings.Utils;

namespace SchedulerSettings
{
    public static class SettingsUtils
    {
        private static readonly string _programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private static readonly string _settingsFolder = Path.Combine(_programData, "Onevinn", "DeploymentScheduler", "Settings");
        private static readonly string _fileName = Path.Combine(_settingsFolder, "Settings.xml");
        private static Settings _settings;

        public static Settings Settings
        {
            get
            {
                GetSettingsFromFile();
                return _settings;
            }
        }

        private static void GetSettingsFromFile()
        {
            if (File.Exists(_fileName))
            {
                var tempSettings = ReadFromXmlFile<Settings>(_fileName);
                tempSettings.IsDefault = false;
                tempSettings.RestartChecks.PendingFileNameExclusions.RemoveAll(x => string.IsNullOrEmpty(x));
                _settings = tempSettings;
            }
            else
            {
                var tmp = new Settings();
                tmp.RestartChecks.PendingFileNameExclusions.RemoveAll(x => string.IsNullOrEmpty(x));
                _settings = tmp;
            }
        }

        public static Settings GetSettingsFromFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                return ReadFromXmlFile<Settings>(fileName);
            }

            return new Settings();
        }

        public static void WriteSettingsToFile()
        {
            if (!Directory.Exists(_settingsFolder))
            {
                Directory.CreateDirectory(_settingsFolder);
            }

            _settings.RestartChecks.PendingFileNameExclusions.RemoveAll(x => string.IsNullOrEmpty(x));
            WriteToXmlFile(_fileName, _settings);
        }

        public static void WriteSettingsToFile(string fileName, Settings settings)
        {
            settings.RestartChecks.PendingFileNameExclusions.RemoveAll(x => string.IsNullOrEmpty(x));
            WriteToXmlFile(fileName, settings);
        }

        private static void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false)
            where T : new()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));

                using (var writer = new StreamWriter(filePath, append))
                {
                    serializer.Serialize(writer, objectToWrite);
                }
            }
            catch { }
        }

        public static void WriteStringToSettingsFile(string settings)
        {
            File.WriteAllText(_fileName, settings);
        }

        private static T ReadFromXmlFile<T>(string filePath)
            where T : new()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));

                using (var reader = new StreamReader(filePath))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch { }

            return default;
        }

        public static string SettingsToString(Settings settings)
        {
            settings.RestartChecks.PendingFileNameExclusions.RemoveAll(x => string.IsNullOrEmpty(x));
            var strSettings = SerializeXmlToString(settings);
            return StringCompressor.CompressString(strSettings);
        }

        private static string SerializeXmlToString<T>(T objectToWrite)
            where T : new()
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(T));

                using (var textWriter = new StringWriter())
                {
                    xmlSerializer.Serialize(textWriter, objectToWrite);
                    return textWriter.ToString();
                }
            }
            catch { }

            return null;
        }

        public static T StringToSettings<T>(string str)
            where T : new()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));

                using (var reader = new StringReader(str))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch { }

            return default;
        }
    }
}
