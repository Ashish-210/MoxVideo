using MoxVideo.Models;
using MoxVideo.Service;
using Newtonsoft.Json;
using System.Text;

public class ElevenLabsSpeechToText
{
  
    private readonly HttpClient _httpClient;  
    private readonly FfMpegWrapper _ffMpegWrapper;
    private readonly TextTranslate _textTranslate;
    private readonly LanguageCodeHelper _languageCoderHelper;
    private readonly ElevenLabsCloningService _elevenLabsCloning;
    private const string BaseTTSUrl = "https://api.elevenlabs.io/v1";
    public readonly log4net.ILog log = log4net.LogManager.GetLogger("RollingFile");
    WhisperVttService whisperService = new WhisperVttService();

    public ElevenLabsSpeechToText(FfMpegWrapper ffMpegWrapper,
        TextTranslate textTranslate,
        IHttpClientFactory factory,
        ElevenLabsCloningService elevenLabsCloning,
        LanguageCodeHelper languageCodeHelper
        )
    {
       
       
        _httpClient = _httpClient = factory.CreateClient("ElevenLabs");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _ffMpegWrapper = ffMpegWrapper;
        _textTranslate = textTranslate;
        _elevenLabsCloning = elevenLabsCloning;
        _languageCoderHelper = languageCodeHelper;
    }
    public async Task<SpeechToTextResponse?> ConvertSpeechToTextAsync(string mp3filePath, string fileName)
    {
        var audioData = await System.IO.File.ReadAllBytesAsync(mp3filePath);       
        using var form = new MultipartFormDataContent();
        // Add the audio file
        using var audioContent = new ByteArrayContent(audioData);
        audioContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("audio/mpeg");
        form.Add(audioContent, "file", fileName);

        // Add parameters
        form.Add(new StringContent("scribe_v1"), "model_id");
        form.Add(new StringContent("true"), "tag_audio_events");
        form.Add(new StringContent("eng"), "language_code");
        form.Add(new StringContent("true"), "diarize");

        var response = await _httpClient.PostAsync("/v1/speech-to-text", form);

        if (response.IsSuccessStatusCode)
        {
            string jsonfilename=Path.ChangeExtension(fileName, ".json");
            string jsonfilepath = Path.Combine(Environment.CurrentDirectory, "Uploads", jsonfilename);
            if (!File.Exists(jsonfilepath))
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(jsonfilepath, jsonString);
                log.Info("Transcription JSON saved to " + jsonfilepath);
            }
                
            return await response.Content.ReadFromJsonAsync<SpeechToTextResponse>();
        }
        else
        {
            log.Error($"API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            throw new Exception($"API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }
    }
    public async Task<byte[]> TextToSpeech(string text, string voiceId = "pqHfZKP75CvOlQylNhV4")
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/text-to-speech/{voiceId}");
            var body = new
            {
                text = text,
                model_id = "eleven_turbo_v2_5",
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75
                }
            };
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch(Exception ex)
        {
            
            log.Error("Error in Text to Speech" + ex.Message);
            return null;
        }
      
    }
    public async Task<string> ConvertToMp3(string inputFilePath, string outputFilePath)
    {
        try
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"Input audio file not found: {inputFilePath}");
            }
            if (!Directory.Exists(outputFilePath))
            {
                Directory.CreateDirectory(outputFilePath); // Create it if it doesn't exist
            }
            // Create multipart form data
            using var form = new MultipartFormDataContent();

            // Read the audio file and add it to the form
            var fileBytes = await File.ReadAllBytesAsync(inputFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            form.Add(fileContent, "audio", Path.GetFileName(inputFilePath));

            // Make the API request
            var response = await _httpClient.PostAsync("v1/audio-isolation", form);

            if (response.IsSuccessStatusCode)
            {
                string outputFile = $"clean_sample_{Guid.NewGuid()}.wav";
                string outputPath = Path.Combine(outputFilePath, outputFile);
                // Get the processed audio data
                var processedAudioBytes = await response.Content.ReadAsByteArrayAsync();

                // Save to output file
                await File.WriteAllBytesAsync(outputPath, processedAudioBytes);

                Console.WriteLine($"Audio isolation completed successfully. Output saved to: {outputFilePath}");
                return outputPath;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                log.Error($"Voic Isolation API Error: {response.StatusCode} - {errorContent}");
                return "";
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error: {ex.Message}");
            return ex.Message;
        }
    }
    public async Task<string> CreateLangVtt(string mp3filePath, TranslationContext context, string TargetLanguage)
    {
        try
        {
            
            if (!File.Exists(mp3filePath))
            {
                log.Error("File not found: " + mp3filePath);
                throw new FileNotFoundException($"Input source vtt file not found: {mp3filePath}");
            }
            StringBuilder sb = new StringBuilder();
            List<TextChunk> newsegments = new List<TextChunk>();
            foreach (var segment in context.SourceSegments)
            {

                string translatedtext = await _textTranslate.Translate(context.SourceLanguage, TargetLanguage, segment.Text);
                sb.Append(translatedtext);
                TextChunk newChunk = new TextChunk
                {
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime,
                    Text = translatedtext
                };
                newsegments.Add(newChunk);
            }
            string langcode=_languageCoderHelper.GetLanguageCode(TargetLanguage);
            //vtt formation of target files
            //Path.Combine(Environment.CurrentDirectory, "Uploads", $"{baseFileName}_{langcode}.mp3");
            //string vttfile = Path.ChangeExtension(mp3filePath, $"_{langcode}.VTT");
            string baseFileName = Path.GetFileNameWithoutExtension(mp3filePath);
            string targetlanguagePath = Path.Combine(Environment.CurrentDirectory, "Uploads", $"{baseFileName}_{langcode}.VTT");
            context.TranslateVttFilePath = await whisperService.CreateVttfromSegements(newsegments, targetlanguagePath);
            return sb.ToString();
        }catch(Exception ex)
        {
            log.Error("Error in CreateLangVtt" + ex.Message);
            return "";
        }
    }
    public async Task TranslateLanguage(TranslationContext context, string mp3filePath, string TargetLanguage)
    {
        if (context.Transcription == null || string.IsNullOrEmpty(context.Transcription.Text))
        {
            log.Error("Transcription text is empty or null.");
            throw new Exception($"Transcription text is empty or null.");
        }
        string translatedtext=await CreateLangVtt(mp3filePath,context, TargetLanguage);
        log.Info("Translation started for" + TargetLanguage);


    

        string langcode = _languageCoderHelper.GetLanguageCode(TargetLanguage);
        string baseFileName = Guid.NewGuid().ToString();
        log.Info("Started Cloning of voice");
        //Voice res = await _elevenLabsCloning.CreateCloneAsync(Guid.NewGuid().ToString(), TargetLanguage, mp3filePath);
        //if (String.IsNullOrEmpty(res.VoiceId))
        //{
        //    log.Error("Voice cloning failed");
        //    throw new Exception($"Voice cloning failed");
        //}
        //else log.Info("Voice cloned with id" + res.VoiceId);
        //log.Info("Generating text to speech");
        var audioBytes = await TextToSpeech(translatedtext.ToString(), context.VoiceId);
        string tempaudiopath = Path.Combine(Environment.CurrentDirectory, "Uploads", $"{baseFileName}_{langcode}.mp3");
       
        string targetaudiopath = Path.Combine(Environment.CurrentDirectory, "Uploads", $"{Path.GetFileNameWithoutExtension(mp3filePath)}_{langcode}.mp3");
        if (audioBytes != null)
        {
            if (File.Exists(tempaudiopath)) File.Delete(tempaudiopath);
            await System.IO.File.WriteAllBytesAsync(tempaudiopath, audioBytes);
        }
        else
        {
            log.Error("Text to Speech api returns null");
            throw new Exception($"Text to Speech throwing error");
        }

        await _ffMpegWrapper.ConvertToMp3(mp3filePath, tempaudiopath, targetaudiopath, context.FirstWordStart);
        if (File.Exists(tempaudiopath)) File.Delete(tempaudiopath);
        //if (!string.IsNullOrEmpty(res.VoiceId))
        //{
        //    log.Info("Deleting the cloned voice");

        //    var deletevoice = await _elevenLabsCloning.DeleteVoice(res.VoiceId);
        //    if (!deletevoice)
        //    {
        //        log.Error("Failed to delete the cloned voice.");
        //    }

        //}
    }



}


