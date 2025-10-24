using MoxVideo.Models;

namespace MoxVideo.Service
{
    public class VttCreationHandler: TranslationHandler
    {
       
        public VttCreationHandler() { 
         
        }

        public override async Task<TranslationContext> HandleAsync(TranslationContext context)
        {
            var vttfile = Path.ChangeExtension(context.OriginalVideoPath, ".VTT");

            WhisperVttService whisperService = new WhisperVttService();
            vttfile = Path.Combine(Environment.CurrentDirectory, "Uploads", Path.GetFileName(vttfile));
            context.SourceVttFilePath = await whisperService.CreateVttFromJsonTranscript(context.Transcription, vttfile);
            //sourcesegment
            var vttParser = new VttParserService();
             context.SourceSegments = await vttParser.ParseVttFileAsync(context.SourceVttFilePath);
            if (context.OutputFormat == "vtt" && context.SourceLanguage == context.TargetLanguage)
            {
                context.TranslatedAudioPath = context.SourceVttFilePath;
               
                return context;
            }

            if (_nextHandler != null)
            {
                
                return await _nextHandler.HandleAsync(context);
            }
            return context;
        }
    }
}
