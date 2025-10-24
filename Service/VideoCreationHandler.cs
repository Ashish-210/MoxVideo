using MoxVideo.Models;

namespace MoxVideo.Service
{
    public class VideoCreationHandler: TranslationHandler
    {
        private readonly FfMpegWrapper _ffMpegWrapper;
        public VideoCreationHandler(FfMpegWrapper ffMpegWrapper)
        {
            _ffMpegWrapper = ffMpegWrapper;
        }
        public override async Task<TranslationContext> HandleAsync(TranslationContext context)
        {
            try
            {
                if (!context.Success) return context;

                if (context.OutputFormat == "mp3")
                {
                    context.TranslatedAudioPath = context.TranslatedAudioPath;
                    return context;
                }

                var videoPath = await _ffMpegWrapper.MuxVideoWithNewAudioAsync(
                    context.OriginalVideoPath,
                    context.TranslatedAudioPath,
                    Path.Combine(Environment.CurrentDirectory, "Uploads"),
                    context.LangCode,
                    Guid.NewGuid().ToString());

                // Create relative path for frontend
                string relativepath = string.Format("/Uploads/{0}", Path.GetFileName(videoPath));

                context.OutputVideoPath = relativepath;
                

                return context;
            }
            catch (Exception ex)
            {
                context.Success = false;
                context.ErrorMessage = $"Video creation error: {ex.Message}";
                context.Logger?.Error(ex.Message, ex);
                return context;
            }
        }


    }
}
