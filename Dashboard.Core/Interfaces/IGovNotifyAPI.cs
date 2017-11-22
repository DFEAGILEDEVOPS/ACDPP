using System.Collections.Generic;
using Notify.Models;

namespace Dashboard.NetStandard.Classes
{
    public interface IGovNotifyAPI
    {
        /// <summary>
        ///     Used for unit testing only
        /// </summary>
        /// <param name="status"></param>
        void SetStatus(string status);

        Notification SendEmail(string emailAddress, string templateId, Dictionary<string, dynamic> personalisation, bool test = false);

        Notification SendSms(string mobileNumber, string templateId, Dictionary<string, dynamic> personalisation, bool test = false);
    }
}