﻿using System;
using System.Diagnostics;
using Smartflow.Core;
using Smartflow.Demo.Common;

namespace Smartflow.Demo
{
    public class ConsoleLogMessageFilter : IMessageFilter
    {
        public bool AllowMultiple
        {
            get { return false; }
        }

        public int Order
        {
            get { return int.MaxValue; }
        }

        public void OnMessageExecuting(HandlerContext context)
        {
            Console.WriteLine("Processing " + context.MessageType.Name + " ...");
        }

        public void OnMessageExecuted(HandlerContext context)
        {
            Console.WriteLine("Finished " + context.MessageType.Name);
        }
    }


    public class HandlerPerformanceLogFilterAttribute : FilterAttribute, IMessageFilter<NewsSearchCommand>
    {
        public void OnMessageExecuting(HandlerContext<NewsSearchCommand> context)
        {
            var sw = Stopwatch.StartNew();
            context.MetaData["_Stopwatch"] = sw;
        }

        public void OnMessageExecuted(MessageHandledContext<NewsSearchCommand> context)
        {
            var sw = (Stopwatch)context.MetaData["_Stopwatch"];
            sw.Stop();
            Console.WriteLine("Search {0} in {1:#.##}s", context.Message.Query, sw.Elapsed.TotalSeconds);
        }
    }
}
