using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trackings.Domain.Trackings;
using Trackings.Domain.Utils;
using WLS.KafkaMessenger.Services.Interfaces;

namespace Trackings.Services
{

    public class SendKafkaMessageService : BackgroundService
    {
        private readonly ILogger<SendKafkaMessageService> _logger;
        private readonly IServiceScopeFactory _factory;
        private readonly KafkaMessageChannel _channel;

        public SendKafkaMessageService(ILogger<SendKafkaMessageService> logger, KafkaMessageChannel channel, IServiceScopeFactory factory)
        {
            _logger = logger;
            _channel = channel;
            _factory = factory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{instance} is running", this);
            string topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC");
            using (var scope = _factory.CreateScope())
            {
                var messenger = scope.ServiceProvider.GetRequiredService<IKafkaMessengerService>();
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("{instance} is waiting for incoming messages", this);
                    while (await _channel.Reader().WaitToReadAsync(stoppingToken))
                    {
                        while (_channel.Reader().TryRead(out KafkaMessage message))
                        {
                            try
                            {
                                await messenger.SendKafkaMessage(message.Id, message.Subject, message.Data);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "{instance} error while sending the message - id:{id}, subject:{subject}, topic: {topic}", this, message.Id, message.Subject, topic);
                            }
                        }
                    }
                }
            }
            _logger.LogInformation("{instance} is shutting down", this);
        }
    }
}