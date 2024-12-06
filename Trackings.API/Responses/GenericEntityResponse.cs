using System;
using Trackings.Domain;
using Newtonsoft.Json;

namespace Trackings.API.Responses
{
    public abstract class GenericEntityResponse
    {
        [JsonProperty(Order = -2)]
        public Guid Id { get; set; }

        [JsonIgnore]
        [JsonProperty(Order = 100)]
        public int CreatedBy { get; set; }

        [JsonIgnore]
        [JsonProperty(Order = 101)]
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        [JsonProperty(Order = 102)]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty(Order = 103)]
        public LinkSelfResponse _links { get; set; }

        public GenericEntityResponse()
        {
            _links ??= new LinkSelfResponse();
        }
    }
}
