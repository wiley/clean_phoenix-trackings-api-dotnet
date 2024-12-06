using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;
using Trackings.Domain.Utils.CustomValidations;

namespace Trackings.Domain.Trackings
{
    public class TrackingsContext
    {
        [BsonIgnoreIfNull]
        [CustomIdFormatValidation]
        public Guid TrainingProgramId { get; set; }

        [BsonIgnoreIfNull]
        [CustomIdFormatValidation]
        public Guid LoId { get; set; }

        [BsonIgnoreIfNull]
        [CustomIdFormatValidation]
        public Guid AttemptId { get; set; }

        [BsonIgnoreIfNull]
        [CustomIdFormatValidation]
        public Guid EventId { get; set; }

        [BsonIgnoreIfNull]
        [CustomIdFormatValidation]
        public Guid EntitlementId { get; set; }
   
        public int OrganizationId { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
