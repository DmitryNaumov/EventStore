using System;
using System.Collections.Generic;

namespace NeedfulThings.EventStore
{
    public interface IEventStream
    {
        Guid Id { get; }

        int Revision { get; }

        IEnumerable<IEvent> GetCommittedEvents();

        void Persist(ICommit commit);
    }
}