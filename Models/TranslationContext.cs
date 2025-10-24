namespace MoxVideo.Models;

public class TranslationContext
{
    // Input properties
    
    public string SourceLanguage { get; set; }
    public string VoiceId { get; set; }
    public string LangCode { get; set; }
    public string TranslatedAudioPath { get; set; }
    public string TargetLanguage { get; set; }

    // Processing artifacts
    public string OriginalVideoPath { get; set; }
    public string Mp3FilePath { get; set; }
    public SpeechToTextResponse Transcription { get; set; }
    public string SourceVttFilePath { get; set; }
    public string TranslateVttFilePath { get; set; }
    public List<TextChunk> SourceSegments { get; set; }
    public double FirstWordStart { get; set; }

    // Output properties
    public string OutputVideoPath { get; set; }
    public string RelativePath { get; set; }
    public bool Success { get; set; } = true;
    public string ErrorMessage { get; set; }
    public string OutputFormat { get; set; }
    
    public log4net.ILog Logger { get; set; }
}