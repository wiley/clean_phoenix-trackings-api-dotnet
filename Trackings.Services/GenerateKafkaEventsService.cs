using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trackings.Domain.Utils;
using Trackings.Domain.Trackings;
using System.Linq;
using Trackings.Infrastructure.Interface.Mongo;

namespace Trackings.Services
{

    public class GenerateKafkaEventsService : BackgroundService
    {
        private readonly ILogger<GenerateKafkaEventsService> _logger;
        private readonly IServiceScopeFactory _factory;
        private readonly KafkaMessageChannel _channel;
        private readonly GenerateKafkaEventsMutex _mutex;

        public GenerateKafkaEventsService(ILogger<GenerateKafkaEventsService> logger, KafkaMessageChannel channel, GenerateKafkaEventsMutex mutex, IServiceScopeFactory factory)
        {
            _logger = logger;
            _factory = factory;
            _channel = channel;
            _mutex = mutex;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{instance} is running", this);
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("{instance} is waiting for a signal", this);
                await _mutex.WaitAsync(stoppingToken);
                _mutex.Release(); // Re-acquire the lock immediately to prevent multiple GenerateKafkaEvents() tasks from running in parallel
                _logger.LogInformation("{instance} receieved a signal", this);

                try
                {
                    using var scope = _factory.CreateScope();
                    GenerateKafkaEvents(
                        scope.ServiceProvider.GetRequiredService<IMongoRepository<Tracking>>()
                    );
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{instance} exception", this);
                }
                finally
                {
                    _mutex.Wait(); // Release the lock
                }
            }
            _logger.LogInformation("{instance} is shutting down", this);
        }

        private void GenerateKafkaEvents(IMongoRepository<Tracking> repository)
        {
            var recordsProcessedCount = 0;
            var totalRecordsCount = 0;
            DateTime lastCreatedAt = DateTime.Now;
            try
            {
                var firstRecord = repository.AsQueryable()
                    .OrderBy(x => x.CreatedAt)
                    .Take(1)
                    .FirstOrDefault();
                if (null == firstRecord)
                {
                    _logger.LogInformation("GenerateKafkaEvents - Skipping, no records found");
                    return;
                }

                totalRecordsCount = repository.AsQueryable().Count();
                _logger.LogInformation(
                    "GenerateKafkaEvents - Start, first record created at: {createdAt}, total records count: {count}",
                    firstRecord.CreatedAt,
                    totalRecordsCount
                );
                _channel.Send(new KafkaMessage
                {
                    Id = firstRecord.Id.ToString(),
                    Subject = "TrackingUpdated",
                    Data = firstRecord
                });
                recordsProcessedCount++;
                lastCreatedAt = firstRecord.CreatedAt;
                List<Tracking> trackings;
                do
                {
                    trackings = repository.AsQueryable()
                        .OrderBy(x => x.CreatedAt)
                        .Skip(recordsProcessedCount)
                        .Take(100)
                        .ToList();
                    foreach (var tracking in trackings)
                    {
                        _channel.Send(new KafkaMessage
                        {
                            Id = tracking.Id.ToString(),
                            Subject = "TrackingUpdated",
                            Data = tracking
                        });
                        lastCreatedAt = tracking.CreatedAt;
                        recordsProcessedCount++;
                    }
                    _logger.LogDebug(
                        "GenerateKafkaEvents - Generate {count} events, {recordsProcessedCount} out of {totalRecordsCount}, ids: {firstId} -> {lastId}",
                        trackings.Count,
                        recordsProcessedCount,
                        totalRecordsCount,
                        trackings.FirstOrDefault()?.Id,
                        trackings.LastOrDefault()?.Id
                    );
                }
                while (trackings.Count > 0);
                _logger.LogInformation(
                    "GenerateKafkaEvents - End, last record created at: {createdAt}, processed {recordsProcessedCount} out of {totalRecordsCount}",
                    lastCreatedAt,
                    recordsProcessedCount,
                    totalRecordsCount
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    @"GenerateKafkaEvents - Error, last record: {lastCreatedAt},
                    records processed: {recordsProcessedCount}, expected number of records: {totalRecordsCount}",
                    lastCreatedAt,
                    recordsProcessedCount,
                    totalRecordsCount
                );
            }
        }
    }
}