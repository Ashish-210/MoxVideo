using MoxVideo.Models;

namespace MoxVideo.Service
{
    public class TransalationHandlerService : TranslationHandler
    {
        private readonly ElevenLabsSpeechToText _speechToText;
      
        private readonly LanguageCodeHelper _languageCodeHelper;
      
        public TransalationHandlerService(ElevenLabsSpeechToText speechToText, LanguageCodeHelper languageCodeHelper)
        {

            _speechToText = speechToText;
            _languageCodeHelper = languageCodeHelper;
        }

        public override async Task<TranslationContext> HandleAsync(TranslationContext context)
        {
            try
            {
                ;
                context.LangCode = _languageCodeHelper.GetLanguageCode(context.TargetLanguage);
                string baseFileName = Path.GetFileNameWithoutExtension(context.Mp3FilePath);
                
                var audio = Path.Combine(Environment.CurrentDirectory, "Uploads",($"{baseFileName}.mp3"));


                await _speechToText.TranslateLanguage(context, audio, context.TargetLanguage);


                context.TranslatedAudioPath = Path.Combine(Environment.CurrentDirectory, "Uploads", ($"{baseFileName}_{context.LangCode}.mp3"));
                context.Logger.Info("Audio file is generated after translation");
                if (context.OutputFormat == "vtt")
                {
                    context.TranslatedAudioPath = context.TranslateVttFilePath;
                    return context;
                }
                if (_nextHandler != null)
                {

                    return await _nextHandler.HandleAsync(context);
                }
                return context;

            }
            catch (Exception ex)
            {
                context.ErrorMessage = ex.Message;
                return context;
            }

        }
    }
}
