using Newtonsoft.Json;

namespace Trackings.Domain.Trackings
{
    public class TrackingsData
    {
        public string Duration { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

    }
}
