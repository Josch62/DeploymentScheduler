using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public class RebootInformation
    {
        private readonly long _rebootCountdownStartTimeOffset;
        private readonly int _rebootCountdownAlertInterval;
        private readonly int _rebootCountdownInterval;
        private DateTime _disableHideTime;
        private DateTime _rebootDeadline;
        private bool _rebootPending;
        private bool _hardRebootPending;
        private bool _notifyUI;
        private bool _inGracePeriod;

        public int RebootCountdownInterval
        {
            get
            {
                return _rebootCountdownInterval;
            }
        }

        public int RebootCountdownAlertInterval
        {
            get
            {
                return _rebootCountdownAlertInterval;
            }
        }

        public long RebootCountdownStartTimeOffset
        {
            get
            {
                return _rebootCountdownStartTimeOffset;
            }
        }

        public DateTime DisableHideTime
        {
            get
            {
                return _disableHideTime;
            }

            set
            {
                _disableHideTime = value;
            }
        }

        public DateTime RebootDeadline
        {
            get
            {
                return _rebootDeadline;
            }

            set
            {
                _rebootDeadline = value;
            }
        }

        public bool RebootPending
        {
            get
            {
                return _rebootPending;
            }

            set
            {
                _rebootPending = value;
            }
        }

        public bool HardRebootPending
        {
            get
            {
                return _hardRebootPending;
            }

            set
            {
                _hardRebootPending = value;
            }
        }

        public bool NotifyUI
        {
            get
            {
                return _notifyUI;
            }

            set
            {
                _notifyUI = value;
            }
        }

        public bool InGracePeriod
        {
            get
            {
                return _inGracePeriod;
            }

            set
            {
                _inGracePeriod = value;
            }
        }

        public RebootInformation(bool rebootPending, bool hardRebootPending, bool inGracePeriod, DateTime disableHideTime, DateTime rebootDeadline)
        {
            _rebootPending = rebootPending;
            _hardRebootPending = hardRebootPending;
            _disableHideTime = disableHideTime;
            _rebootDeadline = rebootDeadline;
            _inGracePeriod = inGracePeriod;
            _notifyUI = true;

            if (!(inGracePeriod & rebootPending))
            {
                return;
            }

            _rebootCountdownStartTimeOffset = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            _rebootCountdownAlertInterval = Convert.ToInt32((rebootDeadline.ToUniversalTime() - disableHideTime.ToUniversalTime()).TotalSeconds);
            _rebootCountdownInterval = Convert.ToInt32((rebootDeadline.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);

            if (_rebootCountdownInterval >= _rebootCountdownAlertInterval || _rebootCountdownInterval <= 5)
            {
                return;
            }

            _rebootCountdownAlertInterval = _rebootCountdownInterval - 5;
        }
    }
}
