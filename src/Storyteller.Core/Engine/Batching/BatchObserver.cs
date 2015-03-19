﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Storyteller.Core.Grammars;
using Storyteller.Core.Model.Persistence;
using Storyteller.Core.Results;

namespace Storyteller.Core.Engine.Batching
{
    public class BatchObserver : IBatchObserver
    {
        private readonly IList<BatchWatcher> _watchers = new List<BatchWatcher>(); 


        public void Handle<T>(T message) where T : IResultMessage
        {
            // Nothing
        }

        public void SpecRequeued(SpecExecutionRequest request)
        {
            // TODO -- more instrumentation here
        }


        public void SpecHandled(SpecExecutionRequest request, SpecResults results)
        {
            _watchers.Each(x => x.SpecHandled(request.Plan, results));
            _watchers.RemoveAll(x => x.IsCompleted());
        }

        public Task<IEnumerable<BatchRecord>> MonitorBatch(IEnumerable<SpecNode> nodes)
        {
            var watcher = new BatchWatcher(nodes);
            _watchers.Add(watcher);

            return watcher.Task;
        }

    }
}