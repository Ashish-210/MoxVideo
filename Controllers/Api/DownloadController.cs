using Microsoft.AspNetCore.Mvc;
using MoxVideo.Service;

namespace MoxVideo.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly YouTubeDownloader? _downloader;
        public DownloadController(YouTubeDownloader downloader)
        {
            _downloader = downloader;
        }
        [Route("DownloadVideo")]
        public async Task<IActionResult> DownloadVideo([FromQuery] string url, [FromQuery] string connectionId)
        {
            string jobId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(connectionId))
                return BadRequest("Missing url or connectionId");
            var fileName = $"{jobId}_yout.mp4";
            string outputFile = Path.Combine(Environment.CurrentDirectory, "Uploads",fileName);

            _ = _downloader?.DownloadAsync(url, outputFile, connectionId); // run in background

            return await Task.Run( ()=> Ok(new { message = "Download started" }));
        }
    }
}
