using System;
using System.Linq;
using NeedfulThings.EventStore;
using NeedfulThings.EventStore.MongoDB;
using NUnit.Framework;
using MongoDB.Driver;

namespace EventStore.Tests
{
    [TestFixture]
    public class MongoEventStreamFixture
    {
        private static MongoDatabase _database = new MongoClient().GetServer().GetDatabase("Tests");

        [Test]
        public void Persists_commit_to_stream()
        {
            var stream = new EventStream(_database, Guid.NewGuid());

            var commit = new Commit(Guid.NewGuid(), Enumerable.Repeat(new TestEvent2(), 42));
            stream.Persist(commit);

            Assert.AreEqual(42, stream.Revision, "Revision");
            Assert.AreEqual(1, stream.BucketNo, "BucketNo");
        }

        [Test]
        public void Loads_events_from_stream()
        {
            var stream = new EventStream(_database, Guid.NewGuid());

            var commit = new Commit(Guid.NewGuid(), Enumerable.Repeat(new TestEvent2(), 42));
            stream.Persist(commit);

            var events = stream.GetCommittedEvents();

            Assert.AreEqual(42, events.Count());
        }

        [Test]
        public void Splits_commits_to_multiple_buckets()
        {
            var stream = new EventStream(_database, Guid.NewGuid());

            int n = EventStream.MaxBucketSize + 1;

            while (n-- > 0)
            {
                var commit = new Commit(Guid.NewGuid(), new [] {new TestEvent2()});
                stream.Persist(commit);
            }

            Assert.AreEqual(2, stream.BucketNo);
            Assert.AreEqual(EventStream.MaxBucketSize + 1, stream.GetCommittedEvents().Count());
        }

        class TestEvent2 : IEvent
        {
        }
    }
}