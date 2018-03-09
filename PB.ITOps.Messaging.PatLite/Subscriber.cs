﻿using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Azure.ServiceBus.Core;
using PB.ITOps.Messaging.PatLite.MessageMapping;
using PB.ITOps.Messaging.PatLite.SubscriberRules;

namespace PB.ITOps.Messaging.PatLite
{
    public class Subscriber
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;
        private readonly MultipleBatchProcessor _multipleBatchProcessor;

        public Subscriber(ILog log,  SubscriberConfiguration config, MultipleBatchProcessor multipleBatchProcessor)
        {
            _log = log;
            _config = config;
            _multipleBatchProcessor = multipleBatchProcessor;
        }

        /// <summary>
        /// ReceiveMessages subscriptions and process messages.
        /// </summary>
        /// <param name="tokenSource"></param>
        /// <param name="handlerAssemblies">Assemblies containing handles, defaults to <code>Assembly.GetCallingAssembly()</code></param>
        public async Task Run(CancellationTokenSource tokenSource = null, Assembly[] handlerAssemblies = null)
        {
            if (await Initialise(handlerAssemblies))
            {
                await ListenForMessages(tokenSource);
            }
        }

        /// <summary>
        /// Creates relevant subscriptions.
        /// </summary>
        /// <param name="handlerAssemblies">Assemblies containing handles, defaults to <code>Assembly.GetCallingAssembly()</code></param>
        public async Task<bool> Initialise(Assembly[] handlerAssemblies)
        {
            if (handlerAssemblies == null || handlerAssemblies.Length == 0)
            {
                throw new ArgumentException("One or more assemblies required", nameof(handlerAssemblies));
            }

            MessageMapper.MapMessageTypesToHandlers(handlerAssemblies);
            var builder = new SubscriptionBuilder(_log, _config, new RuleVersionResolver(handlerAssemblies));
            var messagesTypes = MessageMapper.GetHandledTypes().Select(t => t.FullName).ToArray();

            string handlerName = null;
            if (messagesTypes.Length == 0)
            {
                _log.Warn("Subscriber does not handle any message types");
            }
            else
            {
                var handler = MessageMapper.GetHandlerForMessageType(messagesTypes.First()).HandlerType;
                handlerName = handler.FullName;
            }

            return await builder.Build(messagesTypes, handlerName);
        }

        /// <summary>
        /// Process messages, terminate once the cancellation token is cancelled.
        /// </summary>
        public async Task ListenForMessages(CancellationTokenSource tokenSource = null)
        {
            tokenSource = tokenSource ?? new CancellationTokenSource();
            if (_config.ConcurrentBatches == 0)
            {
                _config.ConcurrentBatches = 1;
            }

            if (_config.ConcurrentBatches < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(_config.ConcurrentBatches),
                    $"Cannot support {_config.ConcurrentBatches} concurrent batches.");
            }

           var receivers = CreateMessageReceivers();

            _log.Info("Listening for messages...");

            await _multipleBatchProcessor.ProcessMessages(receivers, tokenSource);
        }

        private List<IMessageReceiver> CreateMessageReceivers()
        {
            var messageReceivers = new List<IMessageReceiver>();

            var messageReceiverBuilder = new MessageReceiverBuilder(_log, _config);
            foreach (var messageReceiver in messageReceiverBuilder.Build())
            {
                messageReceivers.AddRange(Enumerable.Repeat(messageReceiver, _config.ConcurrentBatches));
            }
            return messageReceivers;
        }
    }
}
