using System.Collections.Generic;
using System;

namespace Trackings.Domain.Trackings
{
    public class TrackingsSearchRequest
    {
        public List<Guid> TrainingProgramIds { get; set; }
        public List<Guid> LoIds { get; set; }
        public List<Guid> AttemptIds { get; set; }
        public List<Guid> EventIds { get; set; }
        public List<Guid> EntitlementIds { get; set; }
        public List<int> OrganizationIds { get; set; }
        public List<int> UserIds { get; set; }
    }
}
