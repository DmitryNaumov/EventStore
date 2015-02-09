using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NeedfulThings.EventStore
{
    public class EventStoreRepository<TAggregate> where TAggregate : IAggregateRoot
    {
        private readonly ConcurrentDictionary<Guid, IEventStream> _streams = new ConcurrentDictionary<Guid, IEventStream>();

        private readonly IEventStore _eventStore;

        public EventStoreRepository(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public TAggregate GetById(Guid id, Func<Guid, TAggregate> factory)
        {
            var aggregate = factory(id);

            var stream = GetStream(id, true); // TODO:
            if (stream == null)
                return default(TAggregate);

            foreach (var @event in stream.GetCommittedEvents())
            {
                aggregate.ApplyEvent(@event, true);
            }

            return aggregate;
        }

        public Guid Save(TAggregate aggregate)
        {
            var commitId = Guid.NewGuid();
            Save(aggregate, commitId);

            return commitId;
        }

        public void Save(TAggregate aggregate, Guid commitId)
        {
            var events = aggregate
                .GetUncommittedEvents()
                .Cast<IEvent>()
                .ToList();

            if (!events.Any())
                return;

            var stream = GetStream(aggregate.Id, true);
            if (stream.Revision != (aggregate.Version - events.Count))
                throw new ConcurrencyException();

            var commit = new Commit(commitId, events);
            
            stream.Persist(commit);
        }

        private IEventStream GetStream(Guid streamId, bool createIfNotExists)
        {
            IEventStream stream;
            if (_streams.TryGetValue(streamId, out stream))
            {
                return stream;
            }

            stream = _eventStore.OpenStream(streamId);
            if (stream != null)
            {
                stream = _streams.GetOrAdd(streamId, stream);
                return stream;
            }

            if (createIfNotExists)
            {
                stream = _streams.GetOrAdd(streamId, id => _eventStore.CreateStream(id));
                return stream;
            }

            return null;
        }
    }
}