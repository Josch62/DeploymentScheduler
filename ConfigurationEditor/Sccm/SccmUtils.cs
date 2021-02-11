using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ConfigurationEditor.Extensions;
using ConfigurationEditor.Logging;
using Microsoft.ConfigurationManagement.ManagementProvider;
using Microsoft.ConfigurationManagement.ManagementProvider.WqlQueryEngine;
using SchedulerSettings;
using SchedulerSettings.Utils;

namespace ConfigurationEditor.Sccm
{
    public static class SccmUtils
    {
        public static ObservableCollection<CMColl> GetDeviceCollections()
        {
            var retList = new ObservableCollection<CMColl>();

            try
            {
                var wmiQuery = "SELECT Name, CollectionID, CollectionVariablesCount FROM SMS_Collection WHERE CollectionType='2' AND IsBuiltIn='0'";
                var result = Globals.Sccm.QueryProcessor.ExecuteQuery(wmiQuery) as WqlQueryResultsObject;

                foreach (WqlResultObject coll in result)
                {
                    retList.Add(new CMColl
                    {
                        Name = coll["Name"].StringValue,
                        CollectionId = coll["CollectionID"].StringValue,
                        CollectionVariablesCount = coll["CollectionVariablesCount"].IntegerValue,
                    });
                }
            }
            catch (SmsQueryException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (SmsException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogType.Error);
            }

            return retList;
        }

        public static void DeploySettings(string collectionId)
        {
            try
            {
                IResultObject collSettings = null;

                var wmiQuery = $"SELECT * FROM SMS_CollectionSettings WHERE CollectionID='{collectionId}'";
                var result = Globals.Sccm.QueryProcessor.ExecuteQuery(wmiQuery) as WqlQueryResultsObject;

                foreach (IResultObject coll in result)
                {
                    collSettings = coll;
                    break;
                }

                if (collSettings == null)
                {
                    collSettings = Globals.Sccm.CreateInstance("SMS_CollectionSettings");
                    collSettings["CollectionID"].StringValue = collectionId;
                    collSettings["LocaleID"].IntegerValue = Thread.CurrentThread.CurrentUICulture.LCID;
                }
                else
                {
                    collSettings.Get();
                }

                var collVars = collSettings.GetArrayItems("CollectionVariables");

                var newList = new List<IResultObject>();

                foreach (var v in collVars)
                {
                    if (!v["Name"].StringValue.Contains("DPLSCH"))
                    {
                        newList.Add(v);
                    }
                }

                var settingsChunks = SettingsUtils.SettingsToString(Globals.Settings).Chunk(3000);

                var varCount = 0;

                foreach (var chunk in settingsChunks)
                {
                    var newVar = Globals.Sccm.CreateEmbeddedObjectInstance("SMS_CollectionVariable");

                    newVar["Name"].StringValue = $"DPLSCH{varCount++:00}";
                    newVar["Value"].StringValue = chunk;

                    newList.Add(newVar);
                }

                collSettings.SetArrayItems("CollectionVariables", newList);
                collSettings.Put();
            }
            catch (SmsQueryException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (SmsException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogType.Error);
            }
        }

        public static ObservableCollection<CMColl> GetDeviceCollectionsWithVariables()
        {
            var retList = new ObservableCollection<CMColl>();
            var collList = new List<CMColl>();

            try
            {
                var wmiQuery = "SELECT Name, CollectionID, CollectionVariablesCount FROM SMS_Collection WHERE CollectionType='2' AND IsBuiltIn='0' AND CollectionVariablesCount > 1";
                var result = Globals.Sccm.QueryProcessor.ExecuteQuery(wmiQuery) as WqlQueryResultsObject;

                foreach (WqlResultObject coll in result)
                {
                    collList.Add(new CMColl
                    {
                        Name = coll["Name"].StringValue,
                        CollectionId = coll["CollectionID"].StringValue,
                        CollectionVariablesCount = coll["CollectionVariablesCount"].IntegerValue,
                    });
                }

                if (collList.Count() == 0)
                {
                    return retList;
                }

                foreach (var coll in collList)
                {
                    var wmiQuery1 = $"SELECT * FROM SMS_CollectionSettings WHERE CollectionID='{coll.CollectionId}'";
                    var result1 = Globals.Sccm.QueryProcessor.ExecuteQuery(wmiQuery1) as WqlQueryResultsObject;

                    foreach (WqlResultObject setting in result1)
                    {
                        setting.Get();
                        var hasSettings = false;

                        var collVars = setting.GetArrayItems("CollectionVariables");

                        foreach (var v in collVars)
                        {
                            if (v["Name"].StringValue.Contains("DPLSCH"))
                            {
                                hasSettings = true;
                                break;
                            }
                        }

                        if (hasSettings)
                        {
                            retList.Add(new CMColl
                            {
                                Name = coll.Name,
                                CollectionId = coll.CollectionId,
                                CollectionVariablesCount = coll.CollectionVariablesCount,
                            });
                        }
                    }
                }
            }
            catch (SmsQueryException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (SmsException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogType.Error);
            }

            return retList;
        }

        public static string GetSettingsFromCollection(string collectionId)
        {
            var nameValue = new Dictionary<string, string>();
            var retStr = string.Empty;

            try
            {
                var wmiQuery = $"SELECT * FROM SMS_CollectionSettings WHERE CollectionID='{collectionId}'";
                var result = Globals.Sccm.QueryProcessor.ExecuteQuery(wmiQuery) as WqlQueryResultsObject;

                foreach (WqlResultObject setting in result)
                {
                    setting.Get();

                    var collVars = setting.GetArrayItems("CollectionVariables");

                    foreach (var v in collVars)
                    {
                        if (v["Name"].StringValue.Contains("DPLSCH"))
                        {
                            nameValue.Add(v["Name"].StringValue, v["Value"].StringValue);
                        }
                    }

                    if (nameValue.Count() == 0)
                    {
                        return retStr;
                    }

                    var sortedVars = nameValue.OrderBy(x => x.Key).ToList();

                    foreach (var pair in sortedVars)
                    {
                        retStr += pair.Value.Trim().TrimEnd('\0');
                    }
                }
            }
            catch (SmsQueryException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (SmsException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogType.Error);
            }

            return StringCompressor.DecompressString(retStr);
        }

        public static void RemoveDeployment(string collectionId)
        {
            try
            {
                WqlResultObject collSettings = null;

                var wmiQuery = $"SELECT * FROM SMS_CollectionSettings WHERE CollectionID='{collectionId}'";
                var result = Globals.Sccm.QueryProcessor.ExecuteQuery(wmiQuery) as WqlQueryResultsObject;

                foreach (WqlResultObject coll in result)
                {
                    collSettings = coll;
                    break;
                }

                if (collSettings == null)
                {
                    return;
                }

                collSettings.Get();

                var collVars = collSettings.GetArrayItems("CollectionVariables");

                var newList = new List<IResultObject>();

                foreach (var v in collVars)
                {
                    if (!v["Name"].StringValue.Contains("DPLSCH"))
                    {
                        newList.Add(v);
                    }
                }

                collSettings.SetArrayItems("CollectionVariables", newList);
                collSettings.Put();
            }
            catch (SmsQueryException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (SmsException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogType.Error);
            }
        }

        public static void TriggerMachinePolicyRequest(string collectionId)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Type", ClientActionType.ClientNotificationRequestMachinePolicyNow },
                    { "TargetCollectionID", collectionId },
                };

                Globals.Sccm.ExecuteMethod("SMS_ClientOperation", "InitiateClientOperation", parameters);
            }
            catch (SmsQueryException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (SmsException ex)
            {
                Logger.Log(ex.Details, LogType.Error);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogType.Error);
            }
        }
    }
}
