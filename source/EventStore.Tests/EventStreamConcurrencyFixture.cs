using System;
using System.Linq;
using MongoDB.Driver;
using NeedfulThings.EventStore;
using NeedfulThings.EventStore.MongoDB;
using NUnit.Framework;

namespace EventStore.Tests
{
    [TestFixture]
    public class EventStreamConcurrencyFixture
    {
        private static MongoDatabase _database = new MongoClient().GetServer().GetDatabase("Tests");

        [Test]
        public void Throws_when_commits_added_to_same_bucket()
        {
            var streamId = Guid.NewGuid();
            
            var stream1 = new EventStream(_database, streamId);
            UpdateStream(stream1);

            // open the same stream
            var stream2 = EventStream.OpenExistingStream(_database, streamId);

            // simulate concurrent updates
            UpdateStream(stream1);

            Assert.Throws<ConcurrencyException>(() => UpdateStream(stream2));
        }

        [Test]
        public void Throws_when_commits_added_to_new_bucket()
        {
            var streamId = Guid.NewGuid();

            var stream1 = new EventStream(_database, streamId);
            UpdateStream(stream1, EventStream.MaxBucketSize);

            // open the same stream
            var stream2 = EventStream.OpenExistingStream(_database, streamId);

            // simulate concurrent updates
            UpdateStream(stream1);

            Assert.Throws<ConcurrencyException>(() => UpdateStream(stream2));
        }

        private void UpdateStream(IEventStream stream, int commitCount = 1)
        {
            while (commitCount-- > 0)
            {
                var commit = new Commit(Guid.NewGuid(), new[] {new TestEvent()});
                stream.Persist(commit);
            }
        }

        class TestEvent : IEvent
        {
        }
    }
}