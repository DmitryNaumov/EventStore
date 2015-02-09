using System;
using MongoDB.Driver;

namespace NeedfulThings.EventStore.MongoDB
{
    public sealed class MongoEventStore : IEventStore
    {
        private readonly MongoDatabase _database = new MongoClient().GetServer().GetDatabase("TryMe");

        public IEventStream CreateStream(Guid streamId)
        {
            return new EventStream(_database, streamId);
        }

        public IEventStream OpenStream(Guid streamId)
        {
            return EventStream.OpenExistingStream(_database, streamId);
        }
    }
}