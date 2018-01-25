using Microsoft.Azure.WebJobs;
using System;

namespace HelloWorld.WebJobs
{
    class Program
    {
        static void Main(string[] args)
        {
            JobHost jobHost = new JobHost();
            jobHost.RunAndBlock();
        }
    }
}
