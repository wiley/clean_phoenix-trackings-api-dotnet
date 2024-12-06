using Trackings.Domain.Trackings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Trackings.API.Requests
{
    public class TrackingsCreateOrUpdateRequest
    {
        [Required]
        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("status")]
        [Range(1, 7, ErrorMessage = "The value of Scope cannot be 0.")]
        public TrackingsStatusEnum Status { get; set; }

        [JsonProperty("timeSpent")]
        public int TimeSpent { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        [Required]
        [JsonProperty("firstAccessDate")]
        public DateTime FirstAccessDate { get; set; }

        [Required]
        [JsonProperty("lastAccessDate")]
        public DateTime LastAccessDate { get; set; }

        [JsonProperty("completionDate")]
        public DateTime? CompletionDate { get; set; }

        [Required]
        [JsonProperty("context")]
        public TrackingsContext Context { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; }
    }
}
