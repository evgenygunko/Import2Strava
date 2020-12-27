using Newtonsoft.Json;

namespace Import2Strava.Models
{
    public class UploadStatusResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("activity_id")]
        public int? ActivityId { get; set; }
    }
}
