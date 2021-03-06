﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber.IntegrationTests.Helpers
{
    public class MockBatchProcessingBehaviour : IBatchProcessingBehaviour
    {
        public static List<string> CalledForMessages = new List<string>();
        private readonly string _correlationId;

        public MockBatchProcessingBehaviour(string correlationId)
        {
            _correlationId = correlationId;
        }

        public async Task Invoke(Func<BatchContext, Task> next, BatchContext context)
        {
            CalledForMessages.Add(_correlationId);
            await next(context).ConfigureAwait(false);
        }
    }
}