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
        internal const int MaxBucketSize = 100;

        private readonly MongoCollection<BsonDocument> _collection;

        private int _revision;
        private int _bucketNo;
        private int _bucketSize;

        public static EventStream OpenExistingStream(MongoDatabase database, Guid streamId)
        {
            var collection = database.GetCollection<BsonDocument>("Commits");
            collection.CreateIndex(IndexKeys.Ascending("StreamId", "BucketNo"), IndexOptions.SetUnique(true));

            var document = LoadStreamHeader(collection, streamId);
            if (document == null)
            {
                return null;
            }

            var revision = document["StreamRevision"].AsInt32;
            var bucketNo = document["BucketNo"].AsInt32;
            var bucketSize = document["BucketSize"].AsInt32;

            return new EventStream(collection, streamId, revision, bucketNo, bucketSize);
        }

        public EventStream(MongoDatabase database, Guid streamId)
        {
            Id = streamId;

            _collection = database.GetCollection<BsonDocument>("Commits");
            _collection.CreateIndex(IndexKeys.Ascending("StreamId", "BucketNo"), IndexOptions.SetUnique(true));
        }

        private EventStream(MongoCollection<BsonDocument> collection, Guid streamId, int revision, int bucketNo, int bucketSize)
        {
            _collection = collection;

            Id = streamId;
            _revision = revision;
            _bucketNo = bucketNo;
            _bucketSize = bucketSize;
        }

        public Guid Id { get; private set; }

        public int Revision
        {
            get
            {
                return _revision;
            }
        }

        public int BucketNo
        {
            get
            {
                return _bucketNo;
            }
        }

        public int BucketSize
        {
            get { return _bucketSize; }
        }

        public void Persist(ICommit commit)
        {
            var revision = Revision + commit.Events.Count();

            if (BucketSize % MaxBucketSize == 0)
            {
                var bucketNo = _bucketNo + 1;

                try
                {
                    _collection.Insert(new BsonDocument
                    {
                        {"StreamId", Id},
                        {"StreamRevision", revision},
                        {"Commits", new BsonArray {ToBsonDocument(commit)}},
                        {"BucketSize", 1},
                        {"BucketNo", bucketNo},
                    });

                    _bucketNo = bucketNo;
                }
                catch (MongoDuplicateKeyException)
                {
                    throw new ConcurrencyException();
                }
            }
            else
            {
                var query = Query.And(
                    Query.EQ("StreamId", Id),
                    Query.EQ("BucketNo", BucketNo),
                    Query.EQ("StreamRevision", Revision)
                    );

                var update = Update
                    .Set("StreamRevision", revision)
                    .Push("Commits", ToBsonDocument(commit))
                    .Inc("BucketSize", 1);

                var args = new FindAndModifyArgs
                {
                    Query = query,
                    Update = update,
                };

                var result = _collection.FindAndModify(args);
                if (result.ModifiedDocument == null)
                    throw new ConcurrencyException();
            }

            _revision = revision;
            _bucketSize = BucketSize + 1;
        }

        public IEnumerable<IEvent> GetCommittedEvents()
        {
            var query = Query.EQ("StreamId", Id);

            var cursor = _collection
                .Find(query)
                .SetSortOrder(SortBy.Ascending("BucketNo"));

            return cursor.SelectMany(bucket => ToEvents(bucket));
        }

        private static BsonDocument LoadStreamHeader(MongoCollection<BsonDocument> collection, Guid streamId)
        {
            var query = Query.EQ("StreamId", streamId);
                
            var lastBucket = collection.Find(query)
                .SetFields(Fields.Include("StreamRevision", "BucketNo", "BucketSize"))
                .SetSortOrder(SortBy.Descending("BucketNo"))
                .SetLimit(1)
                .FirstOrDefault();

            return lastBucket;
        }

        private BsonDocument ToBsonDocument(ICommit commit)
        {
            var events = commit
                .Events
                .Select(SerializeEvent)
                .ToList();

            return new BsonDocument
            {
                { "CommitId", commit.Id },
                { "StreamRevision", Revision + events.Count },
                { "Events", new BsonArray(events)},
            };
        }

        private IEnumerable<IEvent> ToEvents(BsonDocument bucket)
        {
            var commits = bucket["Commits"].AsBsonArray;
            foreach (var commit in commits)
            {
                var bsonArray = commit["Events"] as BsonArray;
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