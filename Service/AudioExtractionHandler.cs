using Microsoft.AspNetCore.Mvc;
using MoxVideo.Models;

namespace MoxVideo.Service
{
    public class AudioExtractionHandler: TranslationHandler
    {
        private readonly FfMpegWrapper _ffMpegWrapper;
        
        public  AudioExtractionHandler(FfMpegWrapper ffMpegWrapper)
        {
            _ffMpegWrapper = ffMpegWrapper;
        }

        public override async  Task<TranslationContext> HandleAsync(TranslationContext context)
        {
            try
            {
                
                context.Mp3FilePath = await _ffMpegWrapper.ConvertToMp3(context.OriginalVideoPath);
                if (context.OutputFormat == "mp4" && context.SourceLanguage == context.TargetLanguage)
                {
                    context.OutputVideoPath = context.OriginalVideoPath;
                    return context;
                }
                if (context.OutputFormat =="mp3" && context.SourceLanguage == context.TargetLanguage)
                {
                    context.TranslatedAudioPath = context.Mp3FilePath;
                    return context;
                }
                   
               
                if (_nextHandler != null)
                    return await _nextHandler.HandleAsync(context);
                 

                return context;
            }
            catch (Exception ex)
            {
                context.Success = false;
                context.ErrorMessage = $"Audio extraction error: {ex.Message}";
                context.Logger?.Error(ex.Message, ex);
                return context;
            }


           
        }

    }
}
