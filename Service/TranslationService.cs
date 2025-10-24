using MoxVideo.Models;

namespace MoxVideo.Service
{
    public class TranslationService
    {
        public readonly log4net.ILog log = log4net.LogManager.GetLogger("RollingFile");
      
        private readonly AudioExtractionHandler _audioExtractionService; 
        private readonly TranscriptionHandler _transcriptionService;
        private readonly VttCreationHandler _vttService;
        private readonly TransalationHandlerService _translationHandlerService;
        private readonly VideoCreationHandler _videoCreationHandler;
        public TranslationService(
            FfMpegWrapper ffMpegWrapper,
            ElevenLabsSpeechToText speechToText,
            LanguageCodeHelper languageCodeHelper,
            ElevenLabsCloningService elevenLabsCloning
            )
        {
            _audioExtractionService = new AudioExtractionHandler(ffMpegWrapper);
            _transcriptionService = new TranscriptionHandler(speechToText, languageCodeHelper);
            _vttService = new VttCreationHandler();
            _translationHandlerService = new TransalationHandlerService(speechToText, languageCodeHelper);
            _videoCreationHandler = new VideoCreationHandler(ffMpegWrapper);
            _audioExtractionService
              .SetNext(_transcriptionService)
              .SetNext(_vttService)
              .SetNext(_translationHandlerService)
              .SetNext(_videoCreationHandler);

        }
        public AudioExtractionHandler AudioExtractionService => _audioExtractionService;
        public TranscriptionHandler TranscriptionService => _transcriptionService;
        public VttCreationHandler VttService => _vttService;
        public TransalationHandlerService TranslationHandlerService => _translationHandlerService;
        public VideoCreationHandler VideoCreationHandler => _videoCreationHandler;
    }
}
