using Trackings.Domain.Trackings;
using Trackings.Domain.Pagination;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trackings.Services.Interfaces
{
    public interface ITrackingsService
    {
        int TotalFound { get; }
        Task InsertTrackingData(Tracking tracking);

        Task<Tracking> UpdateTrackingData(Guid Id, Tracking tracking);

        Tracking GetTrackingData(Guid Id);

        Task<Tracking> DeleteTrackingData(Guid Id);

        Task<List<Tracking>> SearchTrackings(TrackingsSearchRequest request, bool includeData = true);

        void GenerateKafkaEvents();
    }
}