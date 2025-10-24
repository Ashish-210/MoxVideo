using Newtonsoft.Json;

namespace MoxVideo.Models
{
    public class Word
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("end")]
        public double End { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("speaker_id")]
        public string SpeakerId { get; set; }
    }
}
