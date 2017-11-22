using System;
using Dashboard.NetStandard.Core.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Extensions;

namespace Dashboard.NetStandard.Core.Classes
{
    public class AzureClassQueue : IClassQueue
    {

        private readonly CloudQueue  _queue;

        public AzureClassQueue(string connectionString, string queueName)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(queueName)) throw new ArgumentNullException(nameof(queueName));

            // Parse the connection string and return a reference to the storage account.
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create a CloudFileClient object for credentialed access to File storage.
            var queueClient = storageAccount.CreateCloudQueueClient();

            _queue = queueClient.GetQueueReference(queueName);

            // Create the queue if it doesn't already exist
            _queue.CreateIfNotExistsAsync();
        }

        public T Peek<T>(out string messageIdPopReceipt)
        {
            messageIdPopReceipt = null;

            // Get the next message
            var retrievedMessage = _queue.PeekMessageAsync().Result;
            if (retrievedMessage == null) return default(T);
            messageIdPopReceipt = retrievedMessage.Id + ":" + retrievedMessage.PopReceipt;

            var json = retrievedMessage.AsString;
            if (string.IsNullOrWhiteSpace(json)) throw new NullReferenceException("Empty string returned from queue");

            var instance = JsonConvert.DeserializeObject<T>(json);

            if (Equals(instance, default(T))) throw new NullReferenceException("Deserialized message from queue is null");

            return instance;
        }

        //Dequeues the message for 30 seconds beforfe returning to queue
        public T Dequeue<T>(out string messageIdPopReceipt)
        {
            messageIdPopReceipt = null;
            // Get the next message
            var retrievedMessage = _queue.GetMessageAsync().Result;
            if (retrievedMessage==null) return default(T);
            messageIdPopReceipt = retrievedMessage.Id + ":" + retrievedMessage.PopReceipt;

            var json = retrievedMessage.AsString;
            if (string.IsNullOrWhiteSpace(json)) throw new NullReferenceException("Empty string returned from queue");

            var instance = JsonConvert.DeserializeObject<T>(json);

            if (Equals(instance, default(T)))throw new NullReferenceException("Deserialized message from queue is null");

            return instance;
        }

        //Deletes the message from to queue
        public void Delete(string messageIdPopReceipt)
        {
            var messageId = messageIdPopReceipt.BeforeFirst(":");
            if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentNullException(nameof(messageIdPopReceipt),"No messageId found");
            var popReceipt = messageIdPopReceipt.AfterFirst(":");
            if (string.IsNullOrWhiteSpace(popReceipt)) throw new ArgumentNullException(nameof(messageIdPopReceipt), "No popReceipt found");

            // Get the next message
            _queue.DeleteMessageAsync(messageId,popReceipt);
        }

        public void ProcessNext<T>(Action<T> action)
        {
            string messageIdPopReceipt;
            T item=Dequeue<T>(out messageIdPopReceipt);
            if (string.IsNullOrWhiteSpace(messageIdPopReceipt)) return;

            var retrievedMessage = _queue.GetMessageAsync().Result;
            if (retrievedMessage == null) return;

            action(item);

            Delete(messageIdPopReceipt);
        }

        public void Enqueue<T>(T instance)
        {
            if (instance == null || Equals(instance,default(T))) throw new ArgumentNullException(nameof(instance));

            var json = JsonConvert.SerializeObject(instance);

            if (string.IsNullOrWhiteSpace(json)) throw new NullReferenceException("Attempt to add empty string to queue");

            if (json.ToBase64().Length > CloudQueueMessage.MaxMessageSize) throw new ArgumentException($"Base64 of serialized object exceeds maximum message size for queue of {CloudQueueMessage.MaxMessageSize} bytes", nameof(instance));

            // Create a message and add it to the queue.
            var message = new CloudQueueMessage(json);
            
            _queue.AddMessageAsync(message);
        }
    }
}