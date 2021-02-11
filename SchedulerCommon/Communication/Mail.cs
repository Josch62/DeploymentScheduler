using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SchedulerSettings.Models;

namespace SchedulerCommon.Communication
{
    public static class Mail
    {
        public static string SendMail(MailSettings mailSettings, string mailBody)
        {
            try
            {
                var message = new System.Net.Mail.MailMessage();
                message.To.Add(mailSettings.MailTo);
                message.Subject = "UserScheduler Feedback";
                message.From = new System.Net.Mail.MailAddress(UserPrincipal.Current.UserPrincipalName);
                message.Body = mailBody;

                var smtp = new System.Net.Mail.SmtpClient(mailSettings.SmtpServer)
                {
                    Credentials = CredentialCache.DefaultNetworkCredentials,
                };

                smtp.Send(message);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "Success";
        }
    }
}
