using MoxVideo.Models;
using System.Net.Http.Headers;
using System.Text;


public class ElevenLabsCloningService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElevenLabsCloningService> _logger;
    public readonly log4net.ILog log = log4net.LogManager.GetLogger("RollingFile");

    public ElevenLabsCloningService(IHttpClientFactory factory, ILogger<ElevenLabsCloningService> logger)
    {

        _httpClient = factory.CreateClient("ElevenLabs");
        _logger = logger;


    }

    // Method 1: Find an existing clone for a speaker and language
    public async Task<Voice> FindExistingCloneAsync(string speakerId, string targetLanguage)
    {
        try
        {
            var allVoices = await _httpClient.GetFromJsonAsync<VoicesResponse>("v2/voices");
            return allVoices?.Voices?.FirstOrDefault(v =>
                v.Category == "cloned" &&
                v.Labels != null &&
                v.Labels.GetValueOrDefault("original_speaker") == speakerId &&
                v.Labels.GetValueOrDefault("target_language") == targetLanguage
            );
        }
        catch (Exception ex)
        {
            log.Error(ex.Message);
            return null;
        }

    }
    public async Task<string> GetAllVoicesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/voices");
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
               
                return jsonContent;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                log.Error($"Failed to get voices. Status: {response.StatusCode}, Error: {errorContent}");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            log.Error($"Exception occurred: {ex.Message}");
            return string.Empty; 
        }
    }
    // Method 2: Create a new clone
    public async Task<Voice?> CreateCloneAsync(string speakerId,
        string targetLanguage,

        string audioFileName)
    {
        try
        {

            var formData = new MultipartFormDataContent();

            // 1. Add the AUDIO FILE with the correct field name ("files")
            var audioData = await File.ReadAllBytesAsync(audioFileName);
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            // The three parameters for Add() are: content, fieldName, fileName
            formData.Add(audioContent, "files", audioFileName); // ✅ This is the most likely correct way

            // 2. Add the NAME (required)
            formData.Add(new StringContent($"Clone_{speakerId}_{targetLanguage}"), "name");

            // 3. Add the DESCRIPTION (optional but good practice)
            formData.Add(new StringContent($"Automatic clone for dubbing {speakerId} into {targetLanguage}"), "description");

            // 4. Add LABELS correctly - This is the most likely culprit for a 400!
            // The API might expect a single JSON object for all labels, not individual fields.
            var labels = new Dictionary<string, string>
                {
                    { "original_speaker", speakerId },
                    { "target_language", targetLanguage },
                    { "source", "auto_generated" }
                };
            string labelsJson = Newtonsoft.Json.JsonConvert.SerializeObject(labels);
            formData.Add(new StringContent(labelsJson, Encoding.UTF8, "application/json"), "labels"); // ✅ Send as JSON

            // 5. Make the request
            var response = await _httpClient.PostAsync("v1/voices/add", formData);
            if (!response.IsSuccessStatusCode)
            {
                // Read the error response
                var errorContent = await response.Content.ReadAsStringAsync();
                // Log the detailed error message
                log.Error($"ElevenLabs API Error: {response.StatusCode}. Details: {errorContent}");

                // Throw an exception with the API's error message
                throw new Exception($"Failed to create voice clone. API Error: {errorContent}");
            }


            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Voice? res= await response.Content.ReadFromJsonAsync<Voice>();
            return res;

        }
        catch (Exception ex)
        {
            log.Error(ex.Message);
            return null;
        }
    }

    public string GenerateSpeakerId(string audioFilePath)
    {
        // This is a placeholder for a real voice fingerprinting algorithm.
        // For now, we'll use a simple hash of the file metadata.
        var fileInfo = new FileInfo(audioFilePath);
        string uniqueData = $"{fileInfo.Name}_{fileInfo.Length}_{fileInfo.LastWriteTime.Ticks}";
        return "spk_" + Math.Abs(uniqueData.GetHashCode()).ToString();
    }
    
    public async Task<bool> DeleteVoice(string voiceId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/v1/voices/{voiceId}");
            if (response.IsSuccessStatusCode)
            {
                log.Error($"Voice {voiceId} deleted successfully.");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                log.Error($"Failed to delete voice. Status: {response.StatusCode}, Error: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            log.Error($"Exception occurred: {ex.Message}");
            return false;
        }
    }
  

}



