﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite
{
    public static class SubscriptionHelper
    {
        private const string AddressKey = "SubscriptionClientAddress";

        public static ConcurrentQueue<BrokeredMessage> GetMessages(this ConcurrentQueue<SubscriptionClient> clients, int batchSize)
        {
            var messageQueue = new ConcurrentQueue<BrokeredMessage>();
            Task.WaitAll(clients.Select(c => QueueMessages(c, messageQueue, batchSize)).ToArray());
            return messageQueue;
        }

        private static async Task QueueMessages(this SubscriptionClient c, ConcurrentQueue<BrokeredMessage> queueMessages, int batchSize)
        {
            var messages = await c.ReceiveBatchAsync(batchSize, TimeSpan.FromSeconds(1));
            foreach (var message in messages)
            {
                message.Properties.Add(AddressKey, c.MessagingFactory.Address.ToString());
                queueMessages.Enqueue(message);
            }
        }
    }
}