using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net;
using Microsoft.Azure.WebJobs;

namespace HelloWorld.WebJobs
{
    public class Functions
    {
        public static void QueuedMethod([QueueTrigger("queue-name")]string queueMessage, TextWriter log)
        {
            log.WriteLine($"Executed {nameof(QueuedMethod)} successfully");
        }

        public static void TimerMethod([TimerTrigger("00:00:10:00")]TimerInfo timer, TextWriter log)
        {
            log.WriteLine($"Executed {nameof(TimerMethod)} successfully");
        }
    }
}
