using System;
using System.Collections.Generic;
using System.Linq;

namespace NeedfulThings.EventStore
{
    public abstract class AggregateRoot : IAggregateRoot
    {
        private readonly List<object> _uncommitedEvents = new List<object>();

        protected AggregateRoot()
        {
            Id = Guid.NewGuid();
        }

        protected AggregateRoot(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }

        public int Version { get; private set; }

        public IEnumerable<object> GetUncommittedEvents()
        {
            var @events = _uncommitedEvents.ToList();

            _uncommitedEvents.Clear();

            return @events;
        }

        public void ApplyEvent(IEvent @event, bool replay)
        {
            var @dynamic = this as dynamic;
            @dynamic.ApplyEvent(@event as dynamic);

            if (!replay)
            {
                _uncommitedEvents.Add(@event);
            }

            Version += 1;
        }

        private void ApplyEvent(IEvent @event)
        {
            var message = string.Format("Missing member:\nvoid ApplyEvent({0} @event)", @event.GetType());
            throw new InvalidProgramException(message);
        }
    }
}