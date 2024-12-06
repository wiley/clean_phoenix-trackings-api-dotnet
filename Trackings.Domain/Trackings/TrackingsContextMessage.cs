using System;

namespace Trackings.Domain.Trackings
{
    public class TrackingsContextMessage
    {
        public Guid TrainingProgramId { get; set; }

        public Guid LoId { get; set; }

        public Guid AttemptId { get; set; }

        public Guid EventId { get; set; }

        public Guid EntitlementId { get; set; }

        public int OrganizationId { get; set; }

        public bool ShouldSerializeTrainingProgramId()
        {
            return TrainingProgramId != Guid.Empty;
        }

        public bool ShouldSerializeLoId()
        {
            return LoId != Guid.Empty;
        }
        public bool ShouldSerializeAttemptId()
        {
            return AttemptId != Guid.Empty;
        }
        public bool ShouldSerializeEventId()
        {
            return EventId != Guid.Empty;
        }
        public bool ShouldSerializeEntitlementId()
        {
            return EntitlementId != Guid.Empty;
        }

        public bool ShouldSerializeOrganizationId()
        {
            return OrganizationId > 0;
        }
    }
}
