using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System;
using Trackings.Domain.Trackings;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Trackings.API.Responses
{
    public class TrackingsResponse : GenericEntityResponse
    {
        public int UserId { get; set; }

        public string Status { get; set; }

        public int TimeSpent { get; set; }

        public int Progress { get; set; }

        public DateTime FirstAccessDate { get; set; }

        public DateTime LastAccessDate { get; set; }

        public DateTime CompletionDate { get; set; }

        public TrackingsContextResponse Context { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public JObject Data { get; set; }

        public bool ShouldSerializeStatus()
        {
            return !string.IsNullOrEmpty(Status);
        }
    }
}
