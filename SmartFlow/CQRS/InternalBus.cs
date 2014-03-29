﻿using System;
using System.Linq;
using Smartflow.Core.Tasks;

namespace Smartflow.Core.CQRS
{
    /// <summary>
    /// This implementation is inspired bu Craig Young's implementation in his CQRS example.
    /// There should be 1 singleton object of this class in the system to act as the internal bus which can send/receive messages and deliver them
    /// to the <see cref="LimitedConcurrencyLevelTaskScheduler"/>
    /// </summary>
    public class InternalBus : ICommandSender, IEventPublisher
    {
        private class Nested
        {
            internal static InternalBus _instance = new InternalBus(null, null);
            static Nested()
            {
            }
        }

        /// <summary>
        /// The current internal bus
        /// </summary>
        public static InternalBus Current
        {
            get { return Nested._instance; }
            set { Nested._instance = value; }
        }


        private readonly ILogger _logger;
        private readonly IHandlerInvoker _handlerInvoker;

        /// <summary>
        /// A callback method to handle once the message is handled
        /// </summary>
        public Action<IMessage> MesageHandled { get; set; }

        /// <summary>
        /// Initialize an internal bus. This object should be singleton
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="handlerInvoker"> </param>
        public InternalBus(ILogger logger, IHandlerInvoker handlerInvoker)
        {
            _logger = logger ?? DependencyResolver.Current.GetService<ILogger>() ?? new NullLogger();
            _handlerInvoker = handlerInvoker ?? DependencyResolver.Current.GetService<IHandlerInvoker>() ?? new DefaultHandlerInvoker();
        }
        
        /// <summary>
        /// Send a command
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        public void Send<T>(T command) where T : Command
        {
            
            var handlers = HandlerProvider.Providers.GetHandlers<T>().ToList();

            if (handlers.Count > 0)
            {
                if (handlers.Count != 1)
                    throw new InvalidOperationException("cannot send to more than one handler");
                var handler1 = handlers[0];

                var priorityTask = new PriorityTask(() =>
                {
                    try
                    {
                        _handlerInvoker.InvokeHandler(handler1, command);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                    finally 
                    {
                        if (MesageHandled != null)
                        {
                            try
                            {
                                MesageHandled(command);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex);
                            }
                        }
                    }
                });

                priorityTask.OnDemand = command.Priority == (uint)MessagePriority.OnDemand;
                priorityTask.Priority = command.Priority;
                priorityTask.Start(PriorityTask.Factory.Scheduler);
            }
            else
            {
                throw new InvalidOperationException("no handler registered");
            }
        }


        /// <summary>
        /// Publish an event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        public void Publish<T>(T @event) where T : Event
        {
            var handlers = HandlerProvider.Providers.GetHandlers<T>();
            foreach (var handler in handlers)
            {
                //dispatch on thread pool for added awesomeness
                var handler1 = handler;
                var priorityTask = new PriorityTask(() =>
                {
                    try
                    {
                        _handlerInvoker.InvokeHandler(handler1, @event);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                    finally
                    {
                        if (MesageHandled != null)
                        {
                            try
                            {
                                //NOTE: This can be fired many times for 1 event object as the event can have many handlers
                                MesageHandled(@event);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex);
                            }
                        }
                    }
                });

                priorityTask.OnDemand = @event.Priority == (uint)MessagePriority.OnDemand;
                priorityTask.Priority = @event.Priority;
                priorityTask.Start(PriorityTask.Factory.Scheduler);
            }
        }
    }
}