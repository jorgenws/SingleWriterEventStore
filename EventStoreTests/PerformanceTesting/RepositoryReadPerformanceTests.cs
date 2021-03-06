﻿using Xunit;
using SimpleEventStore;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using EventStoreTests.HelperClasses;
using Events;
using EventSerialization;

namespace EventStoreTests.PerformanceTesting
{
    public class RepositoryReadPerformanceTests
    {
        //[Fact(Skip = "Performance test")]
        //public void ReadOneMillionFromSqlite()
        //{
        //    int numberOfEvents = 1000000;
        //    int serialId = 0;

        //    IEventStoreBuilder builder = new EventStoreBuilder();
        //    var eventStore = builder.UseSQLiteRepository()
        //                            .Configuration(@"data source=c:\temp\sqliteevents.db;journal_mode=WAL;")
        //                            .UseCustom(new DummyEventPublisher())
        //                            .Build();

        //    var tasks = new ConcurrentBag<Task>();
        //    foreach (int i in Enumerable.Range(0, numberOfEvents))
        //    {
        //        tasks.Add(eventStore.Process(new EventTransaction
        //        {
        //            Events = new[] {
        //            new Event
        //            {
        //                AggregateId = Guid.NewGuid(),
        //                SerialId = serialId++,
        //                SerializedEvent = BitConverter.GetBytes(i),
        //                EventType = "A type of event"
        //            }
        //        }
        //        }));
        //    }

        //    Task.WhenAll(tasks.ToArray()).Wait();

        //    var before = DateTime.Now;

        //    var x = eventStore.GetAllEvents(0, 1000000);

        //    var after = DateTime.Now;

        //    var timeInMilliseconds = (after - before).TotalMilliseconds;
        //    var rate = numberOfEvents / (after - before).TotalSeconds;

        //    eventStore.Dispose();

        //    Assert.True(true, string.Format("Read {0} in {1} milliseconds, which is a rate of {2} per second", numberOfEvents, timeInMilliseconds, rate));
        //}

        [Fact(Skip = "Performance test")]
        public void ReadOneMillionFromLMDB()
        {
            int numberOfEvents = 1000000;
            int serialId = 0;

            IEventStoreBuilder builder = new EventStoreBuilder();
            var eventStore = builder.UseLMDBRepository()
                                    .Configuration(@"c:\temp\lmdbevents", 2, 524288000, new ProtobufEventsSerializer())
                                    .UseCustom(new DummyEventPublisher())
                                    .Build();

            var tasks = new ConcurrentBag<Task>();
            foreach (int i in Enumerable.Range(0, numberOfEvents))
            {
                tasks.Add(eventStore.Process(new EventTransaction
                {
                    Events = new[] {
                    new Event
                    {
                        AggregateId = Guid.NewGuid(),
                        SerialId = serialId++,
                        SerializedEvent = BitConverter.GetBytes(i),
                        EventType = "A type of event"
                    }
                }
                }));
            }

            Task.WhenAll(tasks.ToArray()).Wait();

            var before = DateTime.Now;

            var x = eventStore.GetAllEvents(0, 1000000);

            var after = DateTime.Now;

            var timeInMilliseconds = (after - before).TotalMilliseconds;
            var rate = numberOfEvents / (after - before).TotalSeconds;

            eventStore.Dispose();

            Assert.True(true, string.Format("Read {0} in {1} milliseconds, which is a rate of {2} per second", numberOfEvents, timeInMilliseconds, rate));
        }
    }
}
