using MoxVideo.Models;


namespace MoxVideo.Service
{
    public abstract class TranslationHandler
    {
        protected TranslationHandler _nextHandler;
        public TranslationHandler SetNext(TranslationHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }
        public abstract Task<TranslationContext> HandleAsync(TranslationContext context);
    }
}
