using System;

namespace NeedfulThings.EventStore
{
    public interface IEventStore
    {
        IEventStream OpenStream(Guid streamId);
    }
}