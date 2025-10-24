using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using MoxVideo.Models;
using MoxVideo.Service;
namespace MoxVideo.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslateVideoController : ControllerBase
    {
        public readonly log4net.ILog log = log4net.LogManager.GetLogger("RollingFile");
        private readonly ElevenLabsCloningService _elevenLabsCloning;
        private readonly LanguageCodeHelper _languageCodeHelper   ;

        private readonly FfMpegWrapper _ffMpegWrapper;
      
       
        private readonly TranslationService _translationService;

        public TranslateVideoController(FfMpegWrapper ffMpegWrapper, ElevenLabsSpeechToText speechToText, LanguageCodeHelper languageCodeHelper, ElevenLabsCloningService elevenLabsCloning, TranslationService translationService)
        {

           
            _ffMpegWrapper = ffMpegWrapper;
            _elevenLabsCloning = elevenLabsCloning;
            _translationService = translationService;
            _languageCodeHelper = languageCodeHelper;
        }  
        public async Task<string> ProcessSourceUrl(string source_url, string jobId)
        {
            string localFilePath;
            var maxAllowedSize = 500 * 1024 * 1024; // 500 MB limit



            if (source_url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(source_url, UriKind.Absolute, out Uri uri))
                {
                    throw new ArgumentException("The provided URL is invalid.");
                }

                // Basic security check: ensure it's http or https
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new ArgumentException("Unsupported URL scheme. Only HTTP/HTTPS are allowed.");
                }

                // Create a unique filename using the job ID to avoid conflicts
                var fileName = $"{jobId}_{Path.GetFileName(uri.LocalPath)}";
                // Use a dedicated temp directory, not wwwroot
                var dirPath = Path.Combine(Path.GetTempPath(), "MoxVoiceDownloads");
                Directory.CreateDirectory(dirPath); // Safe to call if exists
                localFilePath = Path.Combine(dirPath, fileName);

                // Download the file
                try
                {
                    using var httpClient = new HttpClient(); // Better than WebClient for async
                                                             // Add a timeout to prevent hanging (e.g., 5 minutes)
                    httpClient.Timeout = TimeSpan.FromMinutes(5);

                    using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode(); // Throws exception if not 200-299

                    // Optional: Check file size from headers before downloading
                    var contentLength = response.Content.Headers.ContentLength;
                    if (contentLength > maxAllowedSize) throw new ArgumentException("File too large.");

                    using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fileStream);
                }
                catch (HttpRequestException ex)
                {
                    throw new FileNotFoundException($"Could not download file from the provided URL. {ex.Message}", ex);
                }
            }
            else
            {
                // Assume it's a local file path
                // SECURITY WARNING: Validate this path thoroughly to prevent directory traversal attacks!
                // For now, we will only allow files from a specific, safe directory.
                var allowedBasePath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "Upload");
                localFilePath = Path.Combine(allowedBasePath, source_url);

                // Check if the resolved path is still inside the allowed directory
                if (!localFilePath.StartsWith(allowedBasePath))
                {
                    throw new ArgumentException("Invalid local file path.");
                }

                if (!System.IO.File.Exists(localFilePath))
                {
                    throw new FileNotFoundException("The specified local file was not found.");
                }
            }
            return localFilePath;
        }
        [HttpGet("GetVoices")]
        public async Task<ContentResult> GetAllVoices()
        {
            var voices = await _elevenLabsCloning.GetAllVoicesAsync();
            return Content(voices, "application/json");
        }
      

        [HttpPost("Translate")]
        public async Task<ContentResult> Translate(
           [FromForm] string? uploaded_mp4file,
           [FromForm] string source_language,
           [FromForm] string voiceid,
           [FromForm] string targetlanguage,
           [FromForm] string output_format
           )
        {
            try
            {
                var originalvideofilepath = Path.Combine(Environment.CurrentDirectory, "Uploads", Path.GetFileName(uploaded_mp4file));
                var context = new TranslationContext
                {
                    OriginalVideoPath= originalvideofilepath,                    
                    SourceLanguage = source_language,
                    VoiceId = voiceid,
                    TargetLanguage = targetlanguage,
                    Logger = log,
                    OutputFormat= output_format  

                };
               
                var result= await _translationService.AudioExtractionService.HandleAsync(context);
                if (result.Success)
                {
                    switch(output_format)
                    {
                        case "mp3":
                                return Content(result.TranslatedAudioPath);
                        case "vtt":
                            return Content(result.TranslateVttFilePath);
                        case "mp4":
                            return Content(result.OutputVideoPath);
                        default:
                            return Content(result.OutputVideoPath);
                            
                        
                    }
                       

                    
                }
                else
                {
                    log.Error(result.ErrorMessage);
                    return Content("");
                }
                return Content(result.RelativePath);
            }
            catch (Exception ex)
            {

                log.Error(ex.Message);
                return Content("");
            }
            return Content("");

        }


        [HttpPost("TranslateApi")]
        public async Task<IActionResult> TranslateApi(
     [FromForm] string? uploaded_mp4file,
     [FromForm] string source_language,
     [FromForm] string voiceid,
     [FromForm] string targetlanguage,
     [FromForm] string output_format)
        {
            try
            {
                var originalvideofilepath = Path.Combine(
                    Environment.CurrentDirectory,
                    "Uploads",
                    Path.GetFileName(uploaded_mp4file));

                var context = new TranslationContext
                {
                    OriginalVideoPath = originalvideofilepath,
                    SourceLanguage = source_language,
                    VoiceId = voiceid,
                    TargetLanguage = targetlanguage,
                    Logger = log,
                    OutputFormat = output_format
                };

                var result = await _translationService.AudioExtractionService.HandleAsync(context);

                if (!result.Success)
                {
                    log.Error(result.ErrorMessage);
                    return BadRequest(new { error = result.ErrorMessage });
                }

                // Determine which file to return based on output format
                string filePath;
                string contentType;
                string fileName;

                switch (output_format)
                {
                    case "mp3":
                        filePath = result.TranslatedAudioPath;
                        contentType = "audio/mpeg";
                        fileName = Path.GetFileName(filePath);
                        break;
                    case "vtt":
                        filePath = result.TranslateVttFilePath;
                        contentType = "text/vtt";
                        fileName = Path.GetFileName(filePath);
                        break;
                    case "mp4":
                    default:
                        filePath = result.OutputVideoPath;
                        contentType = "video/mp4";
                        fileName = Path.GetFileName(filePath);
                        break;
                }

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "File not found" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return StatusCode(500, new { error = "An error occurred during translation" });
            }
        }

        [HttpPost("ReTranslate")]
        public async Task<ContentResult> ReTranslate(
    [FromForm] string? uploaded_mp4file,
    [FromForm] string voiceid,
    [FromForm] string targetlanguage,
    [FromForm] string output_format
)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uploaded_mp4file))
                    return Content(""); 
                var originalvideofilepath = Path.Combine(Environment.CurrentDirectory, "Uploads", Path.GetFileName(uploaded_mp4file));
                // Convert to mp3 if not available
                string mp3filePath = await _ffMpegWrapper.ConvertToMp3(originalvideofilepath);

                // Get transcription from JSON
                string jsonfilename = Path.ChangeExtension(mp3filePath, ".json");
                string jsonfilepath = Path.Combine(Environment.CurrentDirectory, "Uploads", jsonfilename);
                var json = System.IO.File.ReadAllText(jsonfilepath);
                var transcribe = Newtonsoft.Json.JsonConvert.DeserializeObject<SpeechToTextResponse>(json);

                // Get first word start time
                var firstWordStart = (double)transcribe?.Words?.FirstOrDefault()?.Start;

                // Get source VTT file
                string vttfile = Path.ChangeExtension(originalvideofilepath, ".VTT");
                vttfile = Path.Combine(Environment.CurrentDirectory, "Uploads", vttfile);
                var vttParser = new VttParserService();
                var SourceLangCodeFromVtt = await vttParser.GetSourceLang(vttfile);
                var sourcelang = _languageCodeHelper.GetLanguageName(SourceLangCodeFromVtt);
                List<TextChunk> sourceSegments = await vttParser.ParseVttFileAsync(vttfile);

                // Prepare translation context
                var context = new TranslationContext
                {
                    OriginalVideoPath = originalvideofilepath,
                    VoiceId = voiceid,
                    TargetLanguage = targetlanguage,
                    Logger = log,
                    Mp3FilePath = mp3filePath,
                    Transcription = transcribe,
                    FirstWordStart = firstWordStart,
                    SourceLanguage = sourcelang,
                    SourceSegments = sourceSegments,
                    OutputFormat=output_format
                };

                // Start translation chain
                var result = await _translationService.TranslationHandlerService.HandleAsync(context);

                if (result.Success)
                {
                    return Content(result.OutputVideoPath);
                }
                else
                {
                    log.Error(result.ErrorMessage);
                    return Content("");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Content("");
            }
        }

    }
}
