using System.Threading.Channels;
using Trackings.Domain.Trackings;

namespace Trackings.Domain.Utils
{

    public class KafkaMessageChannel
    {
        private readonly Channel<KafkaMessage> _channel = Channel.CreateUnbounded<KafkaMessage>();

        public void Send(KafkaMessage message)
        {
            _channel.Writer.TryWrite(message);
        }

        public ChannelReader<KafkaMessage> Reader()
        {
            return _channel.Reader;
        }
    }
}