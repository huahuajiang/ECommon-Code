using ECommon.Autofac;
using ECommon.JsonNet;
using ECommon.Serilog;
using System;
using ECommonConfiguration = ECommon.Configurations.Configuration;

namespace QuickStart.NameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        static void InitialEQueue()
        {
            var configuration = ECommonConfiguration
                .Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseSerilog()
                .UseJsonNet()
                .RegisterUnhandledExceptionHandler()
                .RegisterEQueueComponent()
        }
    }
}
