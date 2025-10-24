using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MoxVideo.Models
{
    public class TranscribeRequest
    {
        [Required]
        [Url(ErrorMessage = "A valid URL is required for the audio file.")]
        public string source_url { get; set; }
        [Required]
        [Url(ErrorMessage = "Source Language is required")]
        public string source_language { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "At least one target language must be specified.")]
        public string[] target_languages { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "At least one output parameter must be specified.")]
        public OutputType[] output_parameters { get; set; }
    }
    public class Transcribedata
    {
        public string mp3file { get; set; }
        public string language { get; set; }
        public string[] targetlanguages { get; set; }
    }

    public enum OutputType
    {
        None = 0,
        SourceTranscript = 1,     // SRT/VTT in the source language
        TranslatedTranscript = 2, // SRT/VTT in target languages
        TranslatedAudio = 4,      // Audio files in target languages
        TranslatedVideo = 8       // Video files with new audio/subtitles in target languages
    }


    // Response model for the /v1/voices endpoint
    public class VoicesResponse
    {
        [JsonPropertyName("voices")]
        public List<Voice> Voices { get; set; }
    }

    // Voice model
    public class Voice
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } // "premade" or "generated"

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("labels")]
        public Dictionary<string, string> Labels { get; set; } // This is the key for filtering

        [JsonPropertyName("preview_url")]
        public string PreviewUrl { get; set; }
    }
}
