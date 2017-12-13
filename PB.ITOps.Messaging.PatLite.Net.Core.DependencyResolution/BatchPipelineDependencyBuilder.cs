using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class BatchPipelineDependencyBuilder
    {
        private readonly ICollection<Type> _batchPipelineBehaviourTypes;

        public BatchPipelineDependencyBuilder(ICollection<Type> batchPipelineBeviourTypes)
        {
            _batchPipelineBehaviourTypes = batchPipelineBeviourTypes;
        }

        public void RegisterTypes(IServiceCollection serviceCollection)
        {
            foreach (var batchPipelineBehaviourType in _batchPipelineBehaviourTypes)
            {
                serviceCollection.AddSingleton(batchPipelineBehaviourType);
            }
        }

        public BatchProcessingBehaviourPipeline Build(IServiceProvider provider)
        {
            var pipeline = new BatchProcessingBehaviourPipeline();
            foreach (var batchPipelineBehaviourType in _batchPipelineBehaviourTypes)
            {
                pipeline.AddBehaviour((IBatchProcessingBehaviour)provider.GetService(batchPipelineBehaviourType));
            }
            return pipeline;
        }
    }
}