using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SchedulerCommon.Communication;
using SchedulerSettings.Models;

namespace UserScheduler.UserControls
{
    /// <summary>
    /// Interaction logic for MailFeedbackControl.xaml
    /// </summary>
    public partial class MailFeedbackControl : UserControl
    {
        private readonly MailSettings _settings;

        public MailFeedbackControl(MailSettings settings)
        {
            InitializeComponent();
            _settings = settings;
        }

        private void BtSendFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TbFeedbackText.Text))
            {
                TbFeedbackText.IsEnabled = false;
                BtSendFeedback.IsEnabled = false;

                var result = Mail.SendMail(_settings, TbFeedbackText.Text);

                if (!result.Equals("Success"))
                {
                    TbFeedbackText.Text = $"{_settings.FailedTextLine1}\n\n{_settings.FailedTextLine2}";
                    Globals.Log.Error("Failed to send feedback - email.");
                }
                else
                {
                    TbFeedbackText.Text = $"{_settings.SuccessTextLine1}\n\n{_settings.SuccessTextLine2}";
                }
            }
        }
    }
}
