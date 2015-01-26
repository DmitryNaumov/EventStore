using System;
using System.Collections.Generic;

namespace NeedfulThings.EventStore
{
    public interface IAggregateRoot
    {
        Guid Id { get; }

        int Version { get; }

        IEnumerable<object> GetUncommittedEvents();

        void ApplyEvent(IEvent @event, bool replay);
    }
}