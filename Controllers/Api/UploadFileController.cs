using log4net;
using Microsoft.AspNetCore.Mvc;

namespace MoxVideo.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadFileController : ControllerBase
    {
        public readonly log4net.ILog log = log4net.LogManager.GetLogger("RollingFile");
        public UploadFileController()
        {
        }
        [HttpGet("UploadInfo")]
        public IActionResult UploadInfo()
        {
            try
            {
                var allowedExtensions = new[] { ".mp4", ".mp3", ".vtt" };
                var uploadFolder = Path.Combine(Environment.CurrentDirectory, "Uploads");
                var files = Directory.GetFiles(uploadFolder)
                    .Where(f => allowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .ToList();

                var fileInfos = files.Select(f =>
                {
                    var info = new FileInfo(f);
                    return new
                    {
                        filename = info.Name,
                        size = info.Length,
                        format = info.Extension.TrimStart('.'),
                        uploaddate = info.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }).ToList();
                return Ok(fileInfos);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return StatusCode(500, "Could not load files from folder");
            }

        }

        [HttpDelete("DeleteFile")]

        public IActionResult DeleteFile(string fileName)
        {
            try
            {
                var filepath = Path.Combine(Environment.CurrentDirectory, "Uploads", fileName);
                if (System.IO.File.Exists(filepath))
                { 
                    System.IO.File.Delete(filepath);
                    return Ok("File Deleted Successffully");
                }
                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine("File not deleted " + ex.Message);
                return Content("File not deleted");
            }

        }
    }
}
