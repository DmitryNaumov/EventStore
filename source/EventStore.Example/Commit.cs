using System;
using System.Collections.Generic;
using System.Linq;

namespace EventStore.Example
{
    public sealed class Commit : ICommit
    {
        private readonly Guid _id;
        private readonly List<DomainEvent> _events;

        public Commit(Guid id, IEnumerable<DomainEvent> events)
        {
            _id = id;
            _events = events.ToList();
        }

        public Guid Id
        {
            get { return _id; }
        }

        public IEnumerable<DomainEvent> Events
        {
            get { return _events; }
        }
    }
}