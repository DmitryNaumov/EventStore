using System;
using System.Collections.Generic;
using System.Linq;

namespace NeedfulThings.EventStore
{
    public sealed class Commit : ICommit
    {
        private readonly Guid _id;
        private readonly List<IEvent> _events;

        public Commit(Guid id, IEnumerable<IEvent> events)
        {
            _id = id;
            _events = events.ToList();
        }

        public Guid Id
        {
            get { return _id; }
        }

        public IEnumerable<IEvent> Events
        {
            get { return _events; }
        }
    }
}