using Newtonsoft.Json;

namespace MoxVideo.Models
{
    public class SpeechToTextResponse
    {
        
        [JsonProperty("language_code")]
        public string LanguageCode { get; set; }

        [JsonProperty("language_probability")]
        public double LanguageProbability { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("words")]
        public Word[] Words { get; set; }
        [JsonProperty("transcription_id")]
        public string TranscriptionId { get; set; }
    }
}
