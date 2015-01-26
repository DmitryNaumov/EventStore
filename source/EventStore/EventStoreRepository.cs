using System;
using System.Linq;

namespace NeedfulThings.EventStore
{
    public class EventStoreRepository<TAggregate> where TAggregate : IAggregateRoot
    {
        private readonly IEventStore _eventStore;

        public EventStoreRepository(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public TAggregate GetById(Guid id, Func<Guid, TAggregate> factory)
        {
            var aggregate = factory(id);

            var stream = _eventStore.OpenStream(id);
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

            var stream = _eventStore.OpenStream(aggregate.Id);
            if (stream.Revision != (aggregate.Version - events.Count))
                throw new ConcurrencyException();

            var commit = new Commit(commitId, events);
            
            stream.Persist(commit);
        }
    }
}