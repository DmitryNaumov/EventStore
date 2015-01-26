using System;
using System.Linq;

namespace EventStore.Example
{
    public abstract class EventStoreRepository<TAggregate> where TAggregate : IAggregateRoot
    {
        private readonly IEventStore _eventStore;

        public TAggregate GetById(Guid id)
        {
            throw new NotImplementedException();
        }

        public void Save(TAggregate aggregate, Guid commitId)
        {
            var events = aggregate
                .GetUncommittedEvents()
                .Select(x => new DomainEvent {Body = x})
                .ToList();

            if (!events.Any())
                return;

            var stream = _eventStore.OpenStream(aggregate.Id);
            if (stream.Revision != aggregate.Version)
                throw new ConcurrencyException();

            var commit = new Commit(aggregate.Id, events);
        }
    }
}