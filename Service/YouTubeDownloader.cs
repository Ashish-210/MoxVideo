namespace MoxVideo.Service
{
    using Microsoft.AspNetCore.SignalR;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class DownloadHub : Hub
    {
        public async Task SendProgress(string jobId, int percent)
        {
            await Clients.All.SendAsync("ReceiveProgress", jobId, percent);
        }
    }
    public class YouTubeDownloader
    {
        private readonly IHubContext<DownloadHub> _hubContext;

        public YouTubeDownloader(IHubContext<DownloadHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task DownloadAsync(string url, string outputFile, string connectionId)
        {
         
            var ytDlpPath = Path.Combine(Environment.CurrentDirectory, "Tools", "yt-dlp.exe");
            //var arguments = $"-f best -o \"{outputFile}\" {url}";
           var arguments = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]\" -o \"{outputFile}\" {url}";

          
           
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            double lastPercent = 0;
            process.ErrorDataReceived += async (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var match = Regex.Match(e.Data, @"(\d+(?:\.\d+)?)%");
                    if (match.Success)
                    {
                        double percent = double.Parse(match.Groups[1].Value);

                        if (percent - lastPercent >= 1) // update every 1% only
                        {
                            lastPercent = percent;
                            await _hubContext.Clients.Client(connectionId)
                                .SendAsync("ProgressUpdate", percent);
                        }
                    }
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            outputFile = string.Format("/Uploads/{0}", Path.GetFileName(outputFile));
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("DownloadComplete", outputFile);
        }
    }

}
