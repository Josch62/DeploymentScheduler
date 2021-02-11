using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using SchedulerCommon.Ccm;
using SchedulerCommon.Common;
using SchedulerCommon.Enums;
using SchedulerCommon.Logging;

namespace SchedulerCommon.Sql
{
    public static class SqlCe
    {
        private static readonly string _programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private static readonly string _dbFolder = Path.Combine(_programData, "Onevinn", "DeploymentScheduler", "Database");
        private static readonly string _dbPath = Path.Combine(_dbFolder, "EnforcementSchedules.sdf");
        private static readonly string _connectionString = $"DataSource=\"{_dbPath}\"; Password=\"22E62D2B-7F88-4E00-BB75-4C1F6642941C\"";
        private static readonly SqlCEEventSource _log = SqlCEEventSource.Log;

        public static void MaintainDatabase()
        {
            try
            {
                if (!Directory.Exists(_dbFolder))
                {
                    Directory.CreateDirectory(_dbFolder);
                }

                if (!File.Exists(_dbPath))
                {
                    using (var en = new SqlCeEngine(_connectionString))
                    {
                        en.CreateDatabase();
                    }

                    GrantAccess(_dbPath);

                    using (var con = new SqlCeConnection(_connectionString))
                    {
                        con.Open();

                        using (var cmd = new SqlCeCommand(@"create table Version(
                                                Id bigint primary key identity(1,1),
                                                UpdateNumber integer not null,
                                                FileName nchar(10) not null,
                                                InstallDate Datetime not null
                                                )", con))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            _log.Information($"Checking DB version status");

            try
            {
                var installedVersion = GetLatestDBVersion();
                var scripts = GetAvailableScripts();

                _log.Information($"Found {scripts.Count()} upgrades. Installed iteration is: {installedVersion}");

                foreach (var script in scripts.OrderBy(x => x))
                {
                    var fileName = script.Replace("SchedulerCommon.Sql.", string.Empty);
                    var scriptversion = Convert.ToUInt32(fileName.Replace(".sql", string.Empty));

                    if (scriptversion <= installedVersion)
                    {
                        continue;
                    }

                    var scriptContent = string.Empty;

                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(script))
                    {
                        var reader = new StreamReader(stream);
                        scriptContent = reader.ReadToEnd();
                    }

                    var cmds = SqlUtility.SplitSqlStatements(scriptContent);

                    foreach (var cmd in cmds)
                    {
                        ExecuteCommand(cmd);
                    }

                    InsertDBVersion(fileName, scriptversion);
                }

                var updatedVersion = GetLatestDBVersion();

                if (updatedVersion != installedVersion)
                {
                    _log.Information($"Installed DB iteration after upgrade is: {updatedVersion}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "It is safe to suppress a warning from this rule if the command text does not contain any user input.")]
        private static void ExecuteCommand(string command)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = command;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void InsertDBVersion(string fileName, uint iteration)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO Version (UpdateNumber, FileName, InstallDate) VALUES (@UPDATENUMBER, @FILENAME, @TIME)", con))
                    {
                        cmd.Parameters.AddWithValue("UPDATENUMBER", Convert.ToInt32(fileName.Replace(".sql", string.Empty)));
                        cmd.Parameters.AddWithValue("FILENAME", fileName);
                        cmd.Parameters.AddWithValue("TIME", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }

                _log.Information($"Updated DB version (Iteration: '{iteration}')");
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static int GetLatestDBVersion()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT UpdateNumber FROM Version ORDER BY UpdateNumber DESC";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return reader.GetInt32(0);
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return 0;
        }

        private static List<string> GetAvailableScripts()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(rn => rn.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase)).OrderBy(x => x).ToList();
        }

        public static void UpdateSupData(string type, string data)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE SupData SET Data = @DATA WHERE Type = @TYPE", con))
                    {
                        cmd.Parameters.AddWithValue("TYPE", type);
                        cmd.Parameters.AddWithValue("DATA", data);
                        cmd.ExecuteNonQuery();
                    }

                    _log.Information($"Updated SupData ({type})");
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static string GetSupData(string type)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Data FROM SupData WHERE Type = @TYPE";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("TYPE", type);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return string.Empty;
        }

        public static List<ScheduledObject> GetPendingSchedules()
        {
            var retList = new List<ScheduledObject>();

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Id, EnfType, objectId, Revision, EnforceTime, Action, HasRaisedConfirm, IsAutoSchedule FROM Schedules WHERE IsEnforced = 0 AND EnforceTime > @TIME";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("TIME", DateTime.Now);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                retList.Add(new ScheduledObject
                                {
                                    Id = reader.GetInt64(0),
                                    EnforcementType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    ObjectId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                    Revision = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                                    EnforcementTime = reader.GetDateTime(4),
                                    Action = reader.IsDBNull(5) ? string.Empty : reader.GetString(5).Trim(),
                                    HasRaisedConfirm = reader.GetBoolean(6),
                                    IsAutoSchedule = reader.GetBoolean(7),
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return retList;
        }

        public static void SetHasRaisedConfirm(long id)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE Schedules SET HasRaisedConfirm = @HASRAISED WHERE Id = @ID", con))
                    {
                        cmd.Parameters.AddWithValue("HASRAISED", true);
                        cmd.Parameters.AddWithValue("ID", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static List<ScheduledObject> GetDueSchedules()
        {
            var retList = new List<ScheduledObject>();

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Id, EnfType, objectId, Revision, Action, IsAutoSchedule FROM Schedules WHERE IsEnforced = 0 AND EnforceTime <= @TIME";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("TIME", DateTime.Now.AddSeconds(5));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                retList.Add(new ScheduledObject
                                {
                                    Id = reader.GetInt64(0),
                                    EnforcementType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    ObjectId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                    Revision = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                                    Action = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim(),
                                    IsAutoSchedule = reader.GetBoolean(5),
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return retList;
        }

        public static bool IsAppScheduled(string scopeId, string revision, out long id)
        {
            id = 0;

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Id FROM Schedules WHERE ObjectId = @SCOPEID AND Revision = @REVISION AND IsEnforced = 0";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("SCOPEID", scopeId);
                        cmd.Parameters.AddWithValue("REVISION", revision);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                id = reader.GetInt64(0);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return false;
        }

        public static List<ScheduledObject> GetEnforcingSchedules()
        {
            var retList = new List<ScheduledObject>();

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Id, EnfType, objectId, Revision, Action, IsAutoSchedule FROM Schedules WHERE IsEnforced = 1 AND EnforceTime >= @TIME";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("TIME", DateTime.Now.AddMinutes(-5));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                retList.Add(new ScheduledObject
                                {
                                    Id = reader.GetInt64(0),
                                    EnforcementType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    ObjectId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                    Revision = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                                    Action = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim(),
                                    IsAutoSchedule = reader.GetBoolean(5),
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return retList;
        }

        public static List<ScheduledObject> GetAllSchedules()
        {
            var retList = new List<ScheduledObject>();

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Id, EnfType, objectId, Revision, Action, IsAutoSchedule FROM Schedules WHERE IsEnforced = 0";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                retList.Add(new ScheduledObject
                                {
                                    Id = reader.GetInt64(0),
                                    EnforcementType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    ObjectId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                    Revision = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                                    Action = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim(),
                                    IsAutoSchedule = reader.GetBoolean(5),
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return retList;
        }

        public static SupSchedule GetNextSupSchedule()
        {
            var retObject = new SupSchedule();

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT TOP(1) Id, EnforceTime FROM Schedules WHERE IsEnforced = 0 AND EnfType = 'SUP' ORDER BY EnforceTime DESC";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                retObject = new SupSchedule
                                {
                                    Id = reader.GetInt64(0),
                                    EnforcementTime = reader.GetDateTime(1),
                                };

                                break;
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return retObject;
        }

        public static void StoreEvaluatedAppIdRevision(EvaluatedApplication eApp)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO EvaluatedApplications (ObjectId, Revision) VALUES (@ID, @REVISION)", con))
                    {
                        cmd.Parameters.AddWithValue("ID", eApp.Id);
                        cmd.Parameters.AddWithValue("REVISION", eApp.Revision);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static bool IsApplicationEvaluated(EvaluatedApplication eApp)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT * FROM EvaluatedApplications WHERE ObjectId = @ID AND Revision = @REV";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("ID", eApp.Id);
                        cmd.Parameters.AddWithValue("REV", eApp.Revision);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return false;
        }

        public static void CreateAppSchedule(DateTime dt, string objectId, string revision, bool isAutoSchedule = false, string action = "I")
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO Schedules (EnfType, IsEnforced, EnforceTime, ObjectId, Revision, Action, IsAutoSchedule) VALUES (@TYPE, @ISENFORCED, @TIME, @OBJECTID, @REVISION, @ACTION, @ISAUTOSCHEDULE)", con))
                    {
                        cmd.Parameters.AddWithValue("TYPE", "APP");
                        cmd.Parameters.AddWithValue("ISENFORCED", false);
                        cmd.Parameters.AddWithValue("TIME", dt);
                        cmd.Parameters.AddWithValue("OBJECTID", objectId);
                        cmd.Parameters.AddWithValue("REVISION", revision);
                        cmd.Parameters.AddWithValue("ACTION", action);
                        cmd.Parameters.AddWithValue("ISAUTOSCHEDULE", isAutoSchedule);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void UpdateAppSchedule(long id, DateTime newDt)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE Schedules SET EnforceTime = @TIME WHERE Id = @ID", con))
                    {
                        cmd.Parameters.AddWithValue("TIME", newDt);
                        cmd.Parameters.AddWithValue("ID", id);
                        cmd.ExecuteNonQuery();
                    }

                    _log.Information("Updated AppSchedule");
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static long CreateSupSchedule(DateTime dt, bool isAutoSchedule = false)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO Schedules (EnfType, IsEnforced, EnforceTime, ObjectId, IsAutoSchedule) VALUES (@TYPE, @ISENFORCED, @TIME, @OBJECTID, @ISAUTOSCHEDULE)", con))
                    {
                        cmd.Parameters.AddWithValue("TYPE", "SUP");
                        cmd.Parameters.AddWithValue("ISENFORCED", false);
                        cmd.Parameters.AddWithValue("TIME", dt);
                        cmd.Parameters.AddWithValue("OBJECTID", string.Empty);
                        cmd.Parameters.AddWithValue("ISAUTOSCHEDULE", isAutoSchedule);
                        cmd.ExecuteNonQuery();
                    }

                    var sql = "SELECT Id FROM Schedules WHERE IsEnforced = 0 AND EnfType = 'SUP' AND EnforceTime = @TIME";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("TIME", dt);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return reader.GetInt64(0);
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return 0;
        }

        public static void SetEnforcedFlag(long id)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE Schedules SET IsEnforced = @ISENFORCED WHERE Id = @ID", con))
                    {
                        cmd.Parameters.AddWithValue("ISENFORCED", true);
                        cmd.Parameters.AddWithValue("ID", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void SetAppEnforcedFlag(string objectId, string revision)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE Schedules SET IsEnforced = @ISENFORCED WHERE ObjectId = @OBJECTID AND Revision = @REVISION", con))
                    {
                        cmd.Parameters.AddWithValue("ISENFORCED", true);
                        cmd.Parameters.AddWithValue("OBJECTID", objectId);
                        cmd.Parameters.AddWithValue("REVISION", revision);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void DeleteAppSchedule(string objectId, string revision)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"DELETE FROM Schedules WHERE ObjectId = @OBJECTID AND Revision = @REVISION", con))
                    {
                        cmd.Parameters.AddWithValue("OBJECTID", objectId);
                        cmd.Parameters.AddWithValue("REVISION", revision);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static DateTime GetLastServiceAck()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT ShowTime FROM LastServiceAck";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return reader.GetDateTime(0);
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return DateTime.MaxValue;
        }

        public static void SetLastServiceAck()
        {
            try
            {
                DeleteLastServiceAck();

                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO LastServiceAck (ShowTime) VALUES (@SHOWTIME)", con))
                    {
                        cmd.Parameters.AddWithValue("SHOWTIME", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void DeleteLastServiceAck()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"DELETE FROM LastServiceAck", con))
                    {
                        var num = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static ServiceSchedule GetServiceSchedule()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT ExecuteTime, IsAcknowledged FROM ServiceSchedules";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return new ServiceSchedule
                                {
                                    ExecuteTime = reader.GetDateTime(0),
                                    IsAcknowledged = reader.GetBoolean(1),
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return null;
        }

        public static void SetServiceSchedule(DateTime dt)
        {
            try
            {
                DeleteServiceSchedule();

                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO ServiceSchedules (ExecuteTime, IsAcknowledged) VALUES (@EXECUTETIME, @ISACKNOWLEDGED)", con))
                    {
                        cmd.Parameters.AddWithValue("EXECUTETIME", dt);
                        cmd.Parameters.AddWithValue("ISACKNOWLEDGED", false);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void SetServiceScheduleAck()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE ServiceSchedules SET IsAcknowledged = @ISACKNOWLEDGED", con))
                    {
                        cmd.Parameters.AddWithValue("ISACKNOWLEDGED", true);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void DeleteServiceSchedule()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"DELETE FROM ServiceSchedules", con))
                    {
                        var num = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static RestartSchedule GetRestartSchedule()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT RestartTime, DeadLine, IsAcknowledged, IsExpress FROM RestartSchedules";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return new RestartSchedule
                                {
                                    RestartTime = reader.GetDateTime(0),
                                    DeadLine = reader.GetDateTime(1),
                                    IsAcknowledged = reader.GetBoolean(2),
                                    IsExpress = reader.GetBoolean(3),
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return null;
        }

        public static void SetRestartSchedule(RestartSchedule schedule)
        {
            try
            {
                DeleteRestartSchedule();

                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO RestartSchedules (RestartTime, DeadLine, IsAcknowledged, IsExpress) VALUES (@RESTARTTIME, @DEADLINE, @ISACKNOWLEDGED, @ISEXPRESS)", con))
                    {
                        cmd.Parameters.AddWithValue("RESTARTTIME", schedule.RestartTime);
                        cmd.Parameters.AddWithValue("DEADLINE", schedule.DeadLine);
                        cmd.Parameters.AddWithValue("ISACKNOWLEDGED", schedule.IsAcknowledged);
                        cmd.Parameters.AddWithValue("ISEXPRESS", schedule.IsExpress);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void DeleteRestartSchedule()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"DELETE FROM RestartSchedules", con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void SetAutoEnforceSchedules(string schedules)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE AutoUpdateSchedule SET Schedules = @SCHEDULES", con))
                    {
                        cmd.Parameters.AddWithValue("SCHEDULES", schedules);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static string GetAutoEnforceSchedules()
        {
            var schedules = string.Empty;

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Schedules FROM AutoUpdateSchedule";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                schedules = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                                break;
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return schedules;
        }

        public static void SetAutoEnforceFlag(bool isEnforcing)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE AutoUpdateSchedule SET IsEnforcing = @STATE", con))
                    {
                        cmd.Parameters.AddWithValue("STATE", isEnforcing);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static bool GetAutoEnforceFlag()
        {
            var state = false;

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT IsEnforcing FROM AutoUpdateSchedule";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                state = reader.GetBoolean(0);
                                break;
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return state;
        }

        public static void SetUpdatesInstallStatusFlag(bool isInstalling)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE UpdatesInstallStatus SET IsInstalling = @STATE", con))
                    {
                        cmd.Parameters.AddWithValue("STATE", isInstalling);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static bool GetUpdatesInstallStatusFlag()
        {
            var state = false;

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT IsInstalling FROM UpdatesInstallStatus";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                state = reader.GetBoolean(0);
                                break;
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return state;
        }

        public static void UpdateDeadline(string scopeId, string revision, DateTime deadline)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE DeadlineBackup SET Deadline = @DEADLINE WHERE ScopeId = @ID AND Revision = @REV", con))
                    {
                        cmd.Parameters.AddWithValue("ID", scopeId);
                        cmd.Parameters.AddWithValue("REV", revision);
                        cmd.Parameters.AddWithValue("DEADLINE", deadline);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void SaveDeadline(string scopeId, string revision, DateTime deadline)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO DeadlineBackup (ScopeId, Revision, Deadline) VALUES (@ID, @REV, @DEADLINE)", con))
                    {
                        cmd.Parameters.AddWithValue("ID", scopeId);
                        cmd.Parameters.AddWithValue("REV", revision);
                        cmd.Parameters.AddWithValue("DEADLINE", deadline);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static bool GetDeadline(string scopeId, string revision, out DateTime dt)
        {
            dt = DateTime.MinValue;

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Deadline FROM DeadlineBackup WHERE ScopeId = @ID AND Revision = @REV";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("ID", scopeId);
                        cmd.Parameters.AddWithValue("REV", revision);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dt = reader.GetDateTime(0);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return false;
        }

        public static void SaveAssignmentsToDB(List<Assignment> assignments)
        {
            try
            {
                DeleteAssignments();

                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    foreach (var assignment in assignments)
                    {
                        if (assignment.ScopeId.Contains("Prohibited") || assignment.ScopeId.Trim().Length > 99)
                        {
                            continue;
                        }

                        using (var cmd = new SqlCeCommand(@"INSERT INTO Assignments (ScopeId, Revision, Purpose) VALUES (@ID, @REV, @PURPOSE)", con))
                        {
                            cmd.Parameters.AddWithValue("ID", assignment.ScopeId);
                            cmd.Parameters.AddWithValue("REV", assignment.Revision);
                            cmd.Parameters.AddWithValue("PURPOSE", (int)assignment.Purpose);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static AssignmentPurpose GetAssignment(string scopeId, string revision)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT Purpose FROM Assignments WHERE ScopeId = @ID AND Revision = @REV";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("ID", scopeId);
                        cmd.Parameters.AddWithValue("REV", revision);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return (AssignmentPurpose)reader.GetInt32(0);
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return AssignmentPurpose.Unknown;
        }

        public static void DeleteAssignments()
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"DELETE FROM Assignments", con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void SetContentStatus(string objectId, string revision, bool isDownloaded, string location)
        {
            if (GetContentStatus(objectId, revision) != null)
            {
                UpdateContentStatus(objectId, revision, isDownloaded, location);
                return;
            }

            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"INSERT INTO ContentStatus (ObjectId, Revision, IsDownloaded, ContentLocation) VALUES (@OBJECTID, @REVISION, @ISDOWNLOADED, @LOCATION)", con))
                    {
                        cmd.Parameters.AddWithValue("OBJECTID", objectId);
                        cmd.Parameters.AddWithValue("REVISION", revision);
                        cmd.Parameters.AddWithValue("ISDOWNLOADED", isDownloaded);
                        cmd.Parameters.AddWithValue("LOCATION", location);
                        cmd.ExecuteNonQuery();
                    }
                }

                _log.Information($"Set ContentStatus for Feature Update = {isDownloaded}");
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static IpuContentInformation GetContentStatus(string objectId, string revision)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    var sql = "SELECT IsDownloaded, ContentLocation, InstallTime FROM ContentStatus WHERE ObjectId = @OBJECTID AND Revision = @REVISION";

                    using (var cmd = new SqlCeCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("OBJECTID", objectId);
                        cmd.Parameters.AddWithValue("REVISION", revision);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return new IpuContentInformation
                                {
                                    IsDownloaded = reader.GetBoolean(0),
                                    Location = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    InstallTime = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }

            return null;
        }

        private static void UpdateContentStatus(string objectId, string revision, bool isDownloaded, string location)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE ContentStatus SET IsDownloaded = @ISDOWNLOADED, ContentLocation = @LOCATION WHERE ObjectId = @OBJECTID AND Revision = @REVISION", con))
                    {
                        cmd.Parameters.AddWithValue("OBJECTID", objectId);
                        cmd.Parameters.AddWithValue("REVISION", revision);
                        cmd.Parameters.AddWithValue("ISDOWNLOADED", isDownloaded);
                        cmd.Parameters.AddWithValue("LOCATION", location);
                        cmd.ExecuteNonQuery();
                    }

                    _log.Information($"Updated ContentStatus for Feature Update = {isDownloaded}");
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void SetIpuInstallTime(string objectId, string revision, DateTime dtInstall)
        {
            try
            {
                using (var con = new SqlCeConnection(_connectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCeCommand(@"UPDATE ContentStatus SET InstallTime = @INSTALLTIME WHERE ObjectId = @OBJECTID AND Revision = @REVISION", con))
                    {
                        cmd.Parameters.AddWithValue("OBJECTID", objectId);
                        cmd.Parameters.AddWithValue("REVISION", revision);
                        cmd.Parameters.AddWithValue("INSTALLTIME", dtInstall);
                        cmd.ExecuteNonQuery();
                    }

                    _log.Information($"Updated IPU InstallTime in DB. {dtInstall:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (SqlCeException ex)
            {
                _log.Error(ex.Message);
            }
        }

        private static void GrantAccess(string fullPath)
        {
            var dInfo = new DirectoryInfo(fullPath);
            var dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Modify, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
        }
    }
}
