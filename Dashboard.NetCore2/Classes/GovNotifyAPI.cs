using System;
using System.Collections.Generic;
using System.Configuration;
using Notify.Client;
using Notify.Models;
using Extensions;

namespace Dashboard.Classes
{
    public class GovNotifyAPI : IGovNotifyAPI
    {
        private const string _clientReference = "GpgAlphaTest";
        private static readonly string _apiKey = AppSettings.GovNotifyApiKey;
        private static readonly string _apiTestKey = AppSettings.GovNotifyApiTestKey;

        public static readonly string WelcomeTemplateId = AppSettings.WelcomeTemplateId;

        public void SetStatus(string status)
        {
            throw new NotImplementedException();
        }

        public Notification SendEmail(string emailAddress, string templateId, Dictionary<string, dynamic> personalisation, bool test = false)
        {
            var client = new NotificationClient(test && !string.IsNullOrWhiteSpace(_apiTestKey) ? _apiTestKey : _apiKey);
            var result = client.SendEmail(emailAddress, templateId, personalisation, _clientReference);
            var notification = client.GetNotificationById(result.id);
            return notification;
        }

        public Notification SendSms(string mobileNumber, string templateId, Dictionary<string, dynamic> personalisation, bool test = false)
        {
            var client = new NotificationClient(test && !string.IsNullOrWhiteSpace(_apiTestKey) ? _apiTestKey : _apiKey);
            var result = client.SendSms(mobileNumber, templateId, personalisation, _clientReference);
            var notification = client.GetNotificationById(result.id);
            return notification;
        }
        public Notification SendLetter(string templateId, Dictionary<string, dynamic> personalisation, bool test = false)
        {
            var client = new NotificationClient(test && !string.IsNullOrWhiteSpace(_apiTestKey) ? _apiTestKey : _apiKey);
            var result = client.SendLetter(templateId,personalisation, _clientReference);
            var notification = client.GetNotificationById(result.id);
            return notification;
        }
    }
}