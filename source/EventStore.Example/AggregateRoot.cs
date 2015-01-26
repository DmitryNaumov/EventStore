using System;
using System.Collections.Generic;

namespace EventStore.Example
{
    public abstract class AggregateRoot<TState> : IAggregateRoot
    {
        private readonly List<object> _uncommittedEvents = new List<object>();

        public Guid Id { get; private set; }

        public int Version { get; private set; }

        public abstract TState GetState();

        public IEnumerable<object> GetUncommittedEvents()
        {
            return _uncommittedEvents.AsReadOnly();
        }
    }
}