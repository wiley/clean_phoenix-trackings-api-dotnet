using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trackings.Services.Interfaces;
using System.Threading;
using WLS.KafkaMessenger.Services.Interfaces;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Linq.Dynamic.Core;
using Trackings.Infrastructure.Interface.Mongo;
using Trackings.Domain.Trackings;
using Trackings.Domain.Utils;

namespace Trackings.Services
{
    public class KafkaService : IKafkaService
    {
        private readonly GenerateKafkaEventsMutex _mutex;
        private readonly KafkaMessageChannel _channel;

        public KafkaService(KafkaMessageChannel channel, GenerateKafkaEventsMutex mutex)
        {
            _channel = channel;
            _mutex = mutex;
        }

        public void SendKafkaMessage(string id, string subject, object data, string topic = "")
        {
            _channel.Send(new KafkaMessage { Id = id, Subject = subject, Data = data });
        }

        public void GenerateKafkaEvents()
        {
            try
            {
                _mutex.Release();
            }
            catch (SemaphoreFullException)
            {
                throw new GenerateKafkaEventsAlreadyRunningException();
            }
        }
    }
}
