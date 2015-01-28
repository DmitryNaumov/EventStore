using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace NeedfulThings.EventStore.MongoDB
{
    internal sealed class EventStream : IEventStream
    {
        private readonly MongoCollection<BsonDocument> _collection;

        private int _revision;
        private int _seqNo;
        private bool _initialized;

        public EventStream(MongoDatabase database, Guid id)
        {
            Id = id;

            _collection = database.GetCollection<BsonDocument>("Commits");
            _collection.CreateIndex("StreamId", "SeqNo");
        }

        public Guid Id { get; private set; }

        public int Revision
        {
            get
            {
                LoadStreamHeader();

                return _revision;
            }
        }

        public int SeqNo
        {
            get
            {
                LoadStreamHeader();

                return _seqNo;
            }
        }

        public void Persist(ICommit commit)
        {
            _collection.Save(ToBsonDocument(commit));
        }

        public IEnumerable<IEvent> GetCommittedEvents()
        {
            var query = Query.EQ("StreamId", Id);

            var events = _collection
                .Find(query)
                .SetSortOrder(SortBy.Ascending("SeqNo"))
                .SelectMany(ToEvents)
                .ToList();

            return events;
        }

        private void LoadStreamHeader()
        {
            if (_initialized)
                return;

            var query = Query.EQ("StreamId", Id);
                
            var lastCommit = _collection.Find(query)
                .SetFields(Fields.Include("StreamRevision", "SeqNo"))
                .SetSortOrder(SortBy.Descending("SeqNo"))
                .SetLimit(1)
                .FirstOrDefault();

            if (lastCommit != null)
            {
                _revision = lastCommit["StreamRevision"].AsInt32;
                _seqNo = lastCommit["SeqNo"].AsInt32;
            }

            _initialized = true;
        }

        private BsonDocument ToBsonDocument(ICommit commit)
        {
            var events = commit
                .Events
                .Select(SerializeEvent)
                .ToList();

            return new BsonDocument
            {
                { "StreamId", Id },
                { "CommitId", commit.Id },
                { "StreamRevision", Revision + events.Count },
                { "SeqNo", SeqNo + 1 },
                { "Events", new BsonArray(events)},
            };
        }

        private IEnumerable<IEvent> ToEvents(BsonDocument document)
        {
            var bsonArray = document["Events"] as BsonArray;
            if (bsonArray == null)
            {
                yield break;
            }

            var documents = bsonArray.Cast<BsonDocument>().ToList();

            foreach (var item in documents)
            {
                var @event = DeserializeEvent(item);
                yield return @event;
            }
        }

        private IEvent DeserializeEvent(BsonDocument document)
        {
            return BsonSerializer.Deserialize<IEvent>(document);
        }

        private BsonDocument SerializeEvent(IEvent @event)
        {
            var document = new BsonDocument();
            var settings = new BsonDocumentWriterSettings
            {
                GuidRepresentation = GuidRepresentation.Standard
            };

            using (var writer = new BsonDocumentWriter(document, settings))
            {
                BsonSerializer.Serialize(writer, typeof(IEvent), @event);
            }

            return document;
        }
    }
}