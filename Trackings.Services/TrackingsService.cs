using AutoMapper;
using DarwinAuthorization.Models;
using DnsClient;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Trackings.Domain.Exceptions;
using Trackings.Domain.Trackings;
using Trackings.Domain.Utils;
using Trackings.Infrastructure.Interface.Mongo;
using Trackings.Services.Interfaces;

namespace Trackings.Services
{
    public class TrackingsService : ITrackingsService
    {
        private readonly IMongoRepository<Tracking> _trackingRepository;
        private readonly IPaginationService<Tracking> _paginationService;
        private readonly ILogger<TrackingsService> _logger;
        private readonly IKafkaService _kafkaService;
        private DarwinAuthorizationContext _authenticationContext;
        private readonly Mapper _mapper_response;

        public TrackingsService(IMongoRepository<Tracking> trackingRepository, ILogger<TrackingsService> logger
            , IPaginationService<Tracking> paginationService, DarwinAuthorizationContext authenticationContext, IKafkaService kafkaService)
        {
            this._trackingRepository = trackingRepository;
            this._logger = logger;
            this._paginationService = paginationService;
            _kafkaService = kafkaService;
            this._authenticationContext = authenticationContext;

            _mapper_response = new Mapper(new MapperConfiguration(cfg => {
                cfg.CreateMap<Tracking, TrackingsMessage>();
                cfg.CreateMap<TrackingsContext, TrackingsContextMessage>();
                cfg.CreateMap<BsonDocument, string>().ConvertUsing<BsonDocumentToStringConverter>();
            }));
        }

        [Obsolete("_paginationService has not been used yet by Trackings-API")]
        public int TotalFound => _paginationService.TotalRecords;

        public async Task InsertTrackingData(Tracking tracking)
        {
            if (ValidateObjects.IsValidEnumValue<TrackingsStatusEnum>(tracking.Status) == false)
            {
                _logger.LogWarning("InsertTrackingData - BadRequest - {Status}", tracking.Status);
                throw new BadRequestException();
            }

            tracking.CreatedBy = _authenticationContext.UserId;
            tracking.CreatedAt = DateTime.Now;
            tracking.UpdatedAt = DateTime.Now;
            tracking.UpdatedBy = _authenticationContext.UserId;
            await _trackingRepository.InsertOneAsync(tracking);

            _logger.LogInformation("InsertTrackingData - {tracking}", tracking);
            TrackingsMessage response = _mapper_response.Map<TrackingsMessage>(tracking);
            _kafkaService.SendKafkaMessage(tracking.Id.ToString(), "TrackingUpdated", response, "ck-phoenix-tracking");
        }

        public Tracking GetTrackingData(Guid Id)
        {
           return _trackingRepository.FindById(Id);
        }

        public async Task<Tracking> UpdateTrackingData(Guid Id, Tracking tracking)
        {
            var firstRecord = _trackingRepository.FindById(Id);
            if (firstRecord == null)
            {
                _logger.LogWarning("UpdateTrackingData - Not Found - {Id}", Id);
                throw new NotFoundException();
            }

            _logger.LogInformation("Updating ID: {firstRecord.Id}", firstRecord.Id);
            if (ValidateObjects.IsValidEnumValue<TrackingsStatusEnum>(tracking.Status) == false)
            {
                _logger.LogWarning("UpdateTrackingData - BadRequest - {Id}, {Status}", Id, tracking.Status);
                throw new BadRequestException();
            }

            tracking.Id = firstRecord.Id;
            tracking.CreatedAt = firstRecord.CreatedAt;
            tracking.CreatedBy = firstRecord.CreatedBy;
            tracking.UpdatedAt = DateTime.Now;
            tracking.UpdatedBy = _authenticationContext.UserId;
            await _trackingRepository.ReplaceOneAsync(tracking);
            _logger.LogInformation("UpdateTrackingData - {tracking}", tracking);
            TrackingsMessage response = _mapper_response.Map<TrackingsMessage>(tracking);
            _kafkaService.SendKafkaMessage(tracking.Id.ToString(), "TrackingUpdated", response, "ck-phoenix-tracking");

            return tracking;
        }

        public Task<Tracking> DeleteTrackingData(Guid id)
        {
            Tracking result = _trackingRepository.DeleteById(id);
            if (result == null)
            {
                _logger.LogWarning("DeleteTrackingData - Not Found - {id}", id);
                throw new NotFoundException();
            }
            var response = _mapper_response.Map<TrackingsMessage>(result);
            _kafkaService.SendKafkaMessage(id.ToString(), "TrackingRemoved", response, "ck-phoenix-tracking");
            return Task.FromResult(result);
        }

        public async Task<List<Tracking>> SearchTrackings(TrackingsSearchRequest request, bool includeData=true)
        {
            var trackings = _trackingRepository.AsQueryable();

            if (request.LoIds != null)
            {
                trackings = trackings.Where(tracking => request.LoIds.Contains(tracking.Context.LoId));
            }

            if (request.TrainingProgramIds != null)
            {
                trackings = trackings.Where(tracking => request.TrainingProgramIds.Contains(tracking.Context.TrainingProgramId));
            }

            if (request.AttemptIds != null)
            {
                trackings = trackings.Where(tracking => request.AttemptIds.Contains(tracking.Context.AttemptId));
            }

            if (request.EventIds != null)
            {
                trackings = trackings.Where(tracking => request.EventIds.Contains(tracking.Context.EventId));
            }

            if (request.EntitlementIds != null)
            {
                trackings = trackings.Where(tracking => request.EntitlementIds.Contains(tracking.Context.EntitlementId));
            }

            if (request.OrganizationIds != null)
            {
                trackings = trackings.Where(tracking => request.OrganizationIds.Contains(tracking.Context.OrganizationId));
            }

            if (request.UserIds != null)
            {
                trackings = trackings.Where(tracking => request.UserIds.Contains(tracking.UserId));
            }

            List<Tracking> trackingListReturn = trackings.ToList();
            if (includeData == false)
            {
                trackingListReturn.ForEach(tracking => tracking.Data = null);
            }
            
            return trackingListReturn;
        }

        public void GenerateKafkaEvents()
        {
            _kafkaService.GenerateKafkaEvents();
        }
    }
}