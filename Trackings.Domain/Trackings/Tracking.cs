using Trackings.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Trackings.Domain.Trackings
{
    [BsonCollection("trackings")]
    [BsonIgnoreExtraElements]
    public class Tracking : GenericEntity
    {
        [BsonIgnoreIfNull]
        public int UserId { get; set; }

        [BsonIgnoreIfNull]
        public TrackingsStatusEnum Status { get; set; }
        
        [BsonIgnoreIfNull]
        public int TimeSpent { get; set; }
        
        [BsonIgnoreIfNull]
        public int Progress { get; set; }
        
        [BsonIgnoreIfNull]
        public DateTime FirstAccessDate { get; set; }
        
        [BsonIgnoreIfNull]
        public DateTime LastAccessDate { get; set; }
        
        [BsonIgnoreIfNull]
        public DateTime CompletionDate { get; set; }
        
        [BsonIgnoreIfNull]
        public TrackingsContext Context { get; set; }
        
        [BsonIgnoreIfNull]
        public BsonDocument Data { get; set; }
    }
}