﻿using Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventStore
{
    public class EventStore : IDisposable
    {
        private readonly BlockingCollection<TransactionTask> _writerQueue;
        private readonly BlockingCollection<EventTransaction> _publisherQueue;
        private readonly PublisherEnqueuer _publisherEnqueuer;
        private readonly IEventRepository _repository;
        private readonly IEventPublisher _publisher;
        private Task _writerRunner;
        private Task _publisherRunner;

        private const int BufferSize = 1000000;

        public EventStore(IEventRepository repository,
                          IEventPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
            _writerQueue = new BlockingCollection<TransactionTask>(BufferSize);
            _publisherQueue = new BlockingCollection<EventTransaction>(BufferSize);
            _publisherEnqueuer = new PublisherEnqueuer(_publisherQueue);
            
            //ToDo: Look into using continuation to catch that the task died and recreate it if possible.
            _writerRunner = Task.Factory.StartNew(() => new PersistenceQueue(_writerQueue, _publisherEnqueuer, _repository).Consume(),
                                      TaskCreationOptions.LongRunning);
            _publisherRunner = Task.Factory.StartNew(() => new PublisherQueue(_publisherQueue, _publisher).Consume(),
                                      TaskCreationOptions.LongRunning);
        }

        public Task<bool> Process(EventTransaction eventTransaction)
        {
            var tcs = new TaskCompletionSource<bool>();
            var transactionTask = new TransactionTask(eventTransaction, tcs);

            if (eventTransaction.Events.Any(c => c.AggregateId == Guid.Empty || string.IsNullOrWhiteSpace(c.EventType) || c.SerializedEvent == null || !c.SerializedEvent.Any()))
                tcs.SetException(new InvalidOperationException("AggregateId, EventType and SerializedEvent can not be empty"));

            //trys to add for ten millisecond. Not sure what is a good timeout here, but at least it wont block indefinitely
            if (!_writerQueue.TryAdd(transactionTask, 10))
                tcs.SetException(new TimeoutException("Timed out while waiting to add to persistence queue"));

            return tcs.Task;
        }

        public IEnumerable<Event> GetEventsForAggregate(Guid aggregateId)
        {
            return _repository.GetEventsForAggregate(aggregateId);
        }

        public IEnumerable<Event> GetEventsForAggregate(Guid aggregateId, int largerThan)
        {
            return _repository.GetEventsForAggregate(aggregateId, largerThan);
        }

        public IEnumerable<Event> GetAllEvents(int from, int to)
        {
            return _repository.GetAllEvents(from, to);
        }

        public void Dispose()
        {
            _writerQueue.CompleteAdding();
            _writerRunner.Wait();
            _publisherQueue.CompleteAdding();
            _publisherRunner.Wait();
            _publisherRunner.Dispose();
        }
    }
}