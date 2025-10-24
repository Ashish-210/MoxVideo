using Microsoft.AspNetCore.Mvc;
using MoxVideo.Service;

namespace MoxVideo.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly FfMpegWrapper _fMpegWrapper;

        public UploadController(FfMpegWrapper fMpegWrapper)
        {
            _fMpegWrapper = fMpegWrapper;
        }

        [HttpPost]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBoundaryLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ContentResult UploadFile()
        {
            
            string UploadUri = string.Empty;
            foreach (IFormFile file in Request.Form.Files)
            {
                var inputStream = file.OpenReadStream();
                string dirPath = Path.Combine(Environment.CurrentDirectory, "Uploads");
                DirectoryInfo info = new DirectoryInfo(dirPath);
                if (!info.Exists)
                {
                    info.Create();
                }
                string path = Path.Combine(dirPath, file.FileName);
                using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
                {
                    inputStream.CopyTo(outputFileStream);
                    try
                    {
                        var mp3file = _fMpegWrapper.ConvertToMp3(path).Result;
                        var jpgFile = _fMpegWrapper.CreateThumb(path).Result;
                        if (Path.GetExtension(file.FileName).ToLower() == ".mov")
                            _ = _fMpegWrapper.MovToMp4(path);
                        // _ = azureCaptionWrapper.Mp3ToText(mp3file);
                    }
                    catch (Exception)
                    {


                    }
                    UploadUri = string.Format("/Upload/{0}", file.FileName);
                }


            }


            return Content(UploadUri);
        }
        [HttpPost("UploadChunk")]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50 MB
        [RequestSizeLimit(52428800)]
        public JsonResult UploadChunk([FromForm] int chunkIndex, [FromForm] int totalChunks, [FromForm] bool isLastChunk)
        {


          
            string UploadUri = string.Empty;
            string IconUri = string.Empty;
            foreach (IFormFile file in Request.Form.Files)
            {
                var inputStream = file.OpenReadStream();
                string dirPath = Path.Combine(Environment.CurrentDirectory, "Uploads");
                DirectoryInfo info = new DirectoryInfo(dirPath);
                if (!info.Exists)
                    info.Create();
                try
                {
                    string path = Path.Combine(dirPath, file.FileName);
                    if (!System.IO.File.Exists(path))
                        using (FileStream outputFileStream = System.IO.File.Create(path))
                            inputStream.CopyTo(outputFileStream);
                    else
                        using (FileStream outputFileStream = new FileStream(path, FileMode.Append))
                            inputStream.CopyTo(outputFileStream);
                    try
                    {
                        if (isLastChunk)
                        {
                            var mp3file = _fMpegWrapper.ConvertToMp3(path).Result;
                            var jpgFile = _fMpegWrapper.CreateThumb(path).Result;
                            if (Path.GetExtension(file.FileName).ToLower() == ".mov")
                                path = _fMpegWrapper.MovToMp4(path).Result;
                        }

                        // _ = azureCaptionWrapper.Mp3ToText(mp3file);
                    }
                    catch (Exception)
                    {


                    }
                    UploadUri = string.Format("/Uploads/{0}", file.FileName);
                    IconUri = string.Format("/Uploads/{0}", Path.GetFileNameWithoutExtension(file.FileName) + ".jpg");
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }



            }
            return new JsonResult(new { vidioUrl = UploadUri, IconUri = IconUri });

        }
    }
}
