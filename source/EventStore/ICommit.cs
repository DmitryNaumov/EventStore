using System;
using System.Collections.Generic;

namespace NeedfulThings.EventStore
{
    public interface ICommit
    {
        Guid Id { get; }

        IEnumerable<IEvent> Events { get; }
    }
}