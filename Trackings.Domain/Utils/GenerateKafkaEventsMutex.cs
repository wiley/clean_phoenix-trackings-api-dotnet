using System.Threading;

namespace Trackings.Domain.Utils
{

    public class GenerateKafkaEventsMutex : SemaphoreSlim
    {
        public GenerateKafkaEventsMutex() : base(0, 1) // Limit the amount of concurrent jobs to 1
        {
        }
    }
}