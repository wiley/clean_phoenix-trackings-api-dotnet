using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;

namespace Trackings.Domain.Trackings
{
    public class TrackingsMessage
    {
        public Guid Id { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; }
        public int TimeSpent { get; set; }
        public int Progress { get; set; }
        public DateTime FirstAccessDate { get; set; }
        public DateTime LastAccessDate { get; set; }
        public DateTime CompletionDate { get; set; }
        public TrackingsContextMessage Context { get; set; }
        public string Data { get; set; }

        public bool ShouldSerializeStatus()
        {
            return !string.IsNullOrEmpty(Status);
        }
    }
}
