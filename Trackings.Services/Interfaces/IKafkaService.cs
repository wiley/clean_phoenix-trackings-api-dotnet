using System.Collections.Generic;
using System.Threading.Tasks;
using WLS.KafkaMessenger;

namespace Trackings.Services.Interfaces
{
    public interface IKafkaService
    {
        void SendKafkaMessage(string id, string subject, object data, string topic = "");
        void GenerateKafkaEvents();
    }
}
