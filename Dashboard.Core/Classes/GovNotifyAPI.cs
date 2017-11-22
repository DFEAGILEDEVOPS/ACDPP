using System;
using System.Collections.Generic;
using System.Configuration;
using Notify.Client;
using Notify.Models;
using Extensions;

namespace Dashboard.NetStandard.Classes
{
    public class GovNotifyAPI : IGovNotifyAPI
    {
        public GovNotifyAPI(string clientReference, string apiKey, string testApiKey=null)
        {
            _clientReference = clientReference;
            _apiKey = apiKey;
            _apiTestKey = testApiKey;
        }

        private readonly string _clientReference;
        private readonly string _apiKey;
        private readonly string _apiTestKey;

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
    }
}