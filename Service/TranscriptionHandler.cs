using MoxVideo.Models;
using MoxVideo.Service;

namespace MoxVideo.Service
{
    public class TranscriptionHandler: TranslationHandler
    {
        private readonly ElevenLabsSpeechToText _speechToText;
        private readonly FfMpegWrapper _ffMpegWrapper;
        private readonly LanguageCodeHelper _languageCodeHelper;
       
        public TranscriptionHandler( ElevenLabsSpeechToText speechToText, LanguageCodeHelper languageCodeHelper)
        {

            _speechToText = speechToText;
         
            _languageCodeHelper = languageCodeHelper;
        }

         public override async Task<TranslationContext> HandleAsync(TranslationContext context)
        {
            var originalvideofilepath = Path.Combine(Environment.CurrentDirectory, "Uploads", Path.GetFileName(context.Mp3FilePath));
     
            context.Transcription = await _speechToText.ConvertSpeechToTextAsync(context.Mp3FilePath, context.Mp3FilePath);
            var transcript = context.Transcription?.Text ?? string.Empty;
            if (context.SourceLanguage == "Auto")
            {
                if (string.IsNullOrEmpty(context.Transcription.LanguageCode))
                    context.SourceLanguage = "English"; // Default to English if detection fails
                else
                    context.SourceLanguage = _languageCodeHelper.GetLanguageName(context.Transcription.LanguageCode);

            }

            context.FirstWordStart = (double)context.Transcription?.Words?.FirstOrDefault()?.Start;

            await _nextHandler?.HandleAsync(context);
            return context;
        }
    }
}
