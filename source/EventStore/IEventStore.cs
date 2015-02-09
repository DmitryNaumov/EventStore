using System;

namespace NeedfulThings.EventStore
{
    public interface IEventStore
    {
        IEventStream CreateStream(Guid streamId);
        IEventStream OpenStream(Guid streamId);
    }
}