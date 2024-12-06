using DarwinAuthorization.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Trackings.Domain.Exceptions;
using Trackings.Domain.Trackings;
using Trackings.Infrastructure.Interface.Mongo;
using Trackings.Services;
using Trackings.Services.Interfaces;
using Trackings.UnitTest.MockData;

namespace Trackings.UnitTest.Services
{
    public class TrackingsServiceTest
    {
        private readonly ITrackingsService _service;
        private readonly ServiceProvider _services;
        private readonly Mock<IMongoRepository<Tracking>> _repository;
        private readonly Mock<ILogger<TrackingsService>> _logTrackingsService;
        private readonly IPaginationService<Tracking> _paginationTracking;
        private readonly IKafkaService _kafkaService;
        private readonly Mock<DarwinAuthorizationContext> _authorizationContext;

        public TrackingsServiceTest()
        {
            _services = new DIServices().GenerateDependencyInjection();
            _repository = new Mock<IMongoRepository<Tracking>>();
            _logTrackingsService = new Mock<ILogger<TrackingsService>>();
            _paginationTracking = _services.GetService<IPaginationService<Tracking>>();
            _kafkaService = Substitute.For<IKafkaService>();
            _authorizationContext = new Mock<DarwinAuthorizationContext>();
            _service = new TrackingsService(_repository.Object, _logTrackingsService.Object, _paginationTracking, _authorizationContext.Object, _kafkaService);
        }

        [Test]
        public void GetTrackings()
        {
            Tracking mockTrackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            _repository.Setup(r => r.FindById(It.IsAny<Guid>())).Returns(mockTrackings);

            var trackings = _service.GetTrackingData(mockTrackings.Id);
            Assert.AreEqual(trackings, mockTrackings);
        }

        [Test]
        public async Task CreateNewTrackings()
        {
            Tracking newTrackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            await _service.InsertTrackingData(newTrackings);
            _kafkaService.Received(1).SendKafkaMessage(Arg.Any<string>(), "TrackingCreated", Arg.Any<object>(), Arg.Any<string>());
            _repository.Verify(c => c.InsertOneAsync(It.Is<Tracking>(d => d.Id == newTrackings.Id)), Times.Once);
        }

        [Test]
        public async Task CreateNotFoundTrackings()
        {
            Tracking newTrackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            _repository.Setup(c => c.InsertOneAsync(
                    It.IsAny<Tracking>()
                )).Throws(new NotFoundException());

            Assert.ThrowsAsync<NotFoundException>(async () => await _service.InsertTrackingData(newTrackings));
        }

        [Test]
        public async Task UpdateTrackings()
        {
            Tracking mockTrackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            mockTrackings.Status = TrackingsStatusEnum.NOT_STARTED;

            var updatedTrackings = mockTrackings;

            _repository.Setup(r => r.FindById(mockTrackings.Id)).Returns(mockTrackings);
            _repository.Setup(r => r.ReplaceOneAsync(It.IsAny<Tracking>())).Returns(Task.FromResult(updatedTrackings));

            await _service.UpdateTrackingData(mockTrackings.Id, updatedTrackings);
            _kafkaService.Received(1).SendKafkaMessage(Arg.Any<string>(), "TrackingCreated", Arg.Any<object>(), Arg.Any<string>());
            Assert.True(mockTrackings.Status == updatedTrackings.Status);
            Assert.GreaterOrEqual(updatedTrackings.UpdatedAt, mockTrackings.UpdatedAt);
        }

        [Test]
        public async Task UpdateNotFoundTrackings()
        {
            Tracking mockTrackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            mockTrackings.Status = TrackingsStatusEnum.NOT_STARTED;

            var updatedTrackings = mockTrackings;

            _repository.Setup(r => r.FindById(mockTrackings.Id)).Returns(mockTrackings);
            _repository.Setup(r => r.ReplaceOneAsync(It.IsAny<Tracking>())).Throws(new NotFoundException());

            Assert.ThrowsAsync<NotFoundException>(async () => await _service.UpdateTrackingData(mockTrackings.Id, updatedTrackings));
        }

        [Test]
        public async Task DeleteTrackings()
        {
            Tracking mockTrackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            _repository.Setup(r => r.DeleteById(It.IsAny<Guid>())).Returns(mockTrackings);
            
            var trackings = await _service.DeleteTrackingData(mockTrackings.Id);
            _kafkaService.Received(1).SendKafkaMessage(Arg.Any<string>(), "TrackingCreated", Arg.Any<object>(), Arg.Any<string>());
            Assert.AreEqual(trackings, mockTrackings);
        }

        [Test]
        public async Task DeleteNotFoundTrackings()
        {
            Tracking mockTrackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            _repository.Setup(r => r.DeleteById(It.IsAny<Guid>())).Throws(new NotFoundException());

            Assert.ThrowsAsync<NotFoundException>(async () => await _service.DeleteTrackingData(mockTrackings.Id));
        }


        [Test]
        [TestCase(1)]   
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public async Task SearchTrackingsTest(int scenario)
        {
            var searchStructure = TrackingsSearchMockData.ChooseScenario(scenario);
            _repository.Setup(r => r.AsQueryable()).Returns(searchStructure.trackingList.AsQueryable());

            var trackingRequest = searchStructure.trackingSearchRequest;
            var trackingList = await _service.SearchTrackings(trackingRequest);

            Assert.IsNotNull(trackingList);
            Assert.AreEqual(trackingList.Count, searchStructure.expectedReturn);
        }

        [Test]
        public async Task SearchTrackingsTest_ContainsData()
        {
            var searchStructure = TrackingsSearchMockData.ChooseScenario(1);
            _repository.Setup(r => r.AsQueryable()).Returns(searchStructure.trackingList.AsQueryable());

            var trackingRequest = searchStructure.trackingSearchRequest;
            var trackingList = await _service.SearchTrackings(trackingRequest);

            Assert.IsNotNull(trackingList);
            Assert.IsNotNull(trackingList[0].Data);
        }

        [Test]
        public async Task SearchTrackingsTest_NotContainsData()
        {
            var searchStructure = TrackingsSearchMockData.ChooseScenario(1);
            _repository.Setup(r => r.AsQueryable()).Returns(searchStructure.trackingList.AsQueryable());

            var trackingRequest = searchStructure.trackingSearchRequest;
            var trackingList = await _service.SearchTrackings(trackingRequest, false);

            Assert.IsNotNull(trackingList);
            Assert.IsNull(trackingList[0].Data);
        }

    }
}