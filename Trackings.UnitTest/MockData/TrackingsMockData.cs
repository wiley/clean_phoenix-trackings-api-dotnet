using System.Collections.Generic;
using Trackings.Domain.Trackings;
using System;
using System.Diagnostics;
using MongoDB.Bson;

namespace Trackings.UnitTest.MockData
{
    public static class TrackingsMockData
    {

        public static Tracking GetTrackings(Guid id)
        {
            return GenerateTrackings(id);
        }

        private static Tracking GenerateTrackings(Guid id)
        {
            return new Tracking
            {
                Id = id,
                FirstAccessDate = DateTime.Parse("2023-02-22T21:40:18.243Z"),
                LastAccessDate = DateTime.Parse("2023-02-22T21:40:18.243Z"),
                CompletionDate = new DateTime(),
                Status = TrackingsStatusEnum.IN_PROGRESS,
                TimeSpent = 120,
                Progress = 100,
                UserId = 127156,
                Context = new TrackingsContext()
                {
                    TrainingProgramId = Guid.Empty,
                    LoId = Guid.NewGuid(),
                    AttemptId = Guid.Empty,
                    EventId = Guid.Empty,
                    EntitlementId = Guid.Empty,
                    OrganizationId = 1,
                },
                Data = new BsonDocument
                {
                    {
                        "Duration", "1hour"
                    }
                    
                }
            };
        }
    }
}