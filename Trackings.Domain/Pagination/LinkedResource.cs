﻿using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Trackings.Domain.Pagination
{
    public class LinkedResource
    {
        [BsonElement("href")]
        [JsonPropertyName("href")]
        [JsonConverter(typeof(Utils.LinkJsonConverter))]
        public string Href { get; set; }
    }
}