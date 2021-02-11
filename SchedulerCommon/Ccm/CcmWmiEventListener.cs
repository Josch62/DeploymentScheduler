using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using SchedulerCommon.Common;
using SchedulerCommon.Logging;

namespace SchedulerCommon.Ccm
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification ="Lazy")]
    public class CcmWmiEventListener
    {
        public event EventHandler<CcmWmiEventargument> OnStatusChange;

        public event EventHandler<NewApplicationEventArg> OnNewApplication;

        public event EventHandler<NewUpdateEventArg> OnNewUpdate;

        private static readonly WmiEventSource _log = WmiEventSource.Log;
        private readonly ManagementScope _connectionScope;
        private readonly ManagementEventWatcher _eventWatcher;
        private readonly string _namespace_ClientSDK = "ROOT\\ccm\\ClientSDK";
        private DateTime _dt12 = DateTime.MinValue;
        private DateTime _dt21 = DateTime.MinValue;
        private bool _isEvaluation = false;

        public CcmWmiEventListener()
        {
            var path = new ManagementPath
            {
                Server = Environment.MachineName,
                NamespacePath = _namespace_ClientSDK,
            };

            _connectionScope = new ManagementScope(path);
            _connectionScope.Connect();
            _eventWatcher = new ManagementEventWatcher(_connectionScope, new WqlEventQuery("SELECT * FROM CCM_InstanceEvent"));
            _eventWatcher.EventArrived += EventWatcher_EventArrived;
            _eventWatcher.Start();
        }

        private void EventWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var newEvent = e.NewEvent;

            if (newEvent == null)
            {
                return;
            }

            if (!(newEvent["ClassName"].ToString().Equals("CCM_Application") || newEvent["ClassName"].ToString().Equals("CCM_SoftwareUpdate")))
            {
                return;
            }

            try
            {
                var eventObject = (IResultObject)new WmiResultObject(newEvent);
                var target = eventObject["TargetInstancePath"].StringValue;
                var targetparts = target.Split(',');

                switch (eventObject["ActionType"].IntegralValue)
                {
                    case 23:

                        OnStatusChange?.Invoke(this, new CcmWmiEventargument
                        {
                            Id = targetparts[0].Replace("CCM_Application.Id=", string.Empty).Replace("\"", string.Empty),
                            Revision = targetparts[1].Replace("Revision=", string.Empty).Replace("\"", string.Empty),
                            IsMachineTarget = targetparts[2].Replace("IsMachineTarget=", string.Empty).Equals("1"),
                        });
                        break;

                    case 12:

                        if (_dt12 > DateTime.Now)
                        {
                            return;
                        }

                        _dt12 = DateTime.Now.AddMinutes(4);

                        OnNewUpdate?.Invoke(this, new NewUpdateEventArg { ActionId = eventObject["ActionType"].IntegralValue });
                        break;

                    case 21:
                    case 28:

                        if (_dt21 > DateTime.Now || _isEvaluation)
                        {
                            return;
                        }

                        _isEvaluation = true;

                        if (!string.IsNullOrEmpty(eventObject["TargetInstancePath"].StringValue))
                        {
                            var app = CcmUtils.GetSpecificApp(new ScheduledObject
                            {
                                ObjectId = targetparts[0].Replace("CCM_Application.Id=", string.Empty).Replace("\"", string.Empty),
                                Revision = targetparts[1].Replace("Revision=", string.Empty).Replace("\"", string.Empty),
                            });

                            if (app != null)
                            {
                                if (app.Deadline > DateTime.Now.AddMinutes(30))
                                {
                                    _dt21 = DateTime.Now.AddMinutes(4);
                                    OnNewApplication?.Invoke(this, new NewApplicationEventArg { ActionId = eventObject["ActionType"].IntegralValue });
                                }
                            }
                        }

                        _isEvaluation = false;
                        break;
                }

                eventObject.Dispose();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }
    }
}
