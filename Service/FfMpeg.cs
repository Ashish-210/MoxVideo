using System;
using System.Diagnostics;
namespace MoxVideo.Service
{
    public class FfMpegWrapper
    {
        private readonly string fExe = Path.Combine(Environment.CurrentDirectory, "Tools");
        public readonly log4net.ILog log = log4net.LogManager.GetLogger("RollingFile");
        public FfMpegWrapper()
        {
        }
        public async Task<double> GetAudioDuration(string filePath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(fExe, "ffprobe.exe"),
                Arguments = $"-i \"{filePath}\" -show_entries format=duration -v quiet -of csv=\"p=0\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = fExe,
                CreateNoWindow = true
            };

            return await Task.Run(() =>
            {
                using var process = Process.Start(psi);
                if (process == null)
                    return 0d;

                string result = process.StandardOutput?.ReadToEnd() ?? string.Empty;
                process.WaitForExit();
                return double.TryParse(result, out var duration) ? duration : 0;
            });
        }
        public async Task ConvertToMp3(string sourcefile, string tempFile, string targetfile,double delaySeconds)
        {
            try
            {
                double targetDurationSeconds = await GetAudioDuration(sourcefile);
                double actualDuration = await GetAudioDuration(tempFile);
                // Step 3: Compute speed factor
                double speed = actualDuration / targetDurationSeconds;
                if (speed < 0.5) speed = 0.5;
                if (speed > 2.0) speed = 2.0;
                // Step 4: Stretch/compress with ffmpeg
                int delayMs = (int)(delaySeconds * 1000);
                string arguments = $"-y -i \"{tempFile}\" -filter:a \"adelay={delayMs}|{delayMs},atempo={speed}\" \"{targetfile}\"";


              //  string arguments = $"-y -i \"{tempFile}\" -filter:a \"atempo={speed}\" \"{targetfile}\"";
                await RunFfmpegCommandAsync(arguments);
            }
            catch(Exception ex)
            {
                    log.Error(ex.Message);
            }
           
        }
        public async Task<string> ConvertToMp3(string videoFile)
        {
            string audioFile = Path.ChangeExtension(videoFile, ".mp3");

            if (File.Exists(audioFile))
                return audioFile;

            string arguments = $"-y -i \"{videoFile}\" -af \"highpass=f=80,lowpass=f=8000,arnndn=m=sh.rnnn,arnndn=m=mp.rnnn\" \"{audioFile}\"";
            await RunFfmpegCommandAsync(arguments);
            FileInfo outputFileInfo = new FileInfo(audioFile);
            if (outputFileInfo.Length == 0)
            {
                log.Error("Error in converting audio file from video file");
                throw new Exception("Final muxed video file is empty. FFmpeg command failed.");
            }
            return audioFile;
        }
        public async Task<string> MovToMp4(string sourceFile)
        {
            string targetFile = Path.ChangeExtension(sourceFile, ".mp4");
            if (File.Exists(targetFile))
                File.Delete(targetFile);

            ProcessStartInfo startInfo = new()
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{sourceFile}\" -vcodec h264 -pix_fmt yuv420p -acodec aac \"{targetFile}\"",
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,

            };

            var proc = Process.Start(startInfo);
            ArgumentNullException.ThrowIfNull(proc);
            //string output = proc.StandardOutput.ReadToEnd();
            await proc.WaitForExitAsync();
            return targetFile;

        }
        public async Task<string> CreateThumb(string videoFile)
        {
            string jpgFile = Path.ChangeExtension(videoFile, ".jpg");
            if (File.Exists(jpgFile))
                File.Delete(jpgFile);
            string arguments = $"-y -ss 00:00:15 -i \"{videoFile}\" -frames:v 1 \"{jpgFile}\"";
            await RunFfmpegCommandAsync(arguments);
            FileInfo outputFileInfo = new FileInfo(jpgFile);
            if (!outputFileInfo.Exists || outputFileInfo.Length == 0)
            {
                log.Error("Thumbnail creation failed");
                throw new Exception("Thumbnail image file is empty or not created. FFmpeg command failed.");
            }
            return jpgFile;
        }
        public async Task<string> MuxVideoWithNewAudioAsync(string inputVideoPath, string newAudioPath, string outputDirectory, string languageCode, string jobId)
        {
            try
            {

                // Define output path for the final dubbed video
                string baseName = Path.GetFileNameWithoutExtension(inputVideoPath);
                string outputFileName = $"{baseName}_{languageCode}.mp4"; // e.g., "myvideo_es.mp4"
                string outputPath = Path.Combine(outputDirectory, outputFileName);
                //if (File.Exists(outputPath))
                //    return outputPath;

              
                var arguments = $"-i \"{inputVideoPath}\" -i \"{newAudioPath}\" -map 0:v -map 1:a -c:v copy -c:a aac -shortest \"{outputPath}\" -y";
                
                await RunFfmpegCommandAsync(arguments);

                FileInfo outputFileInfo = new FileInfo(outputPath);
                if (outputFileInfo.Length == 0)
                {
                    log.Error("Error in muxing video with new audio");
                    throw new Exception("Final muxed video file is empty. FFmpeg command failed.");
                }

                return outputPath;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return $"Error: {ex.Message}";
            }
        }
        private async Task RunFfmpegCommandAsync(string arguments)
        {
            try
            {

                using (var process = new Process())
                {
                    process.StartInfo.FileName = Path.Combine(fExe, "ffmpeg.exe");
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true; // FFmpeg outputs to stderr

                    process.StartInfo.WorkingDirectory = fExe;
                    process.Start();

                    // Read the output streams (optional, but good for logging)
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    string output = await outputTask;
                    string error = await errorTask;

                    if (process.ExitCode != 0)
                    {
                        log.Error(error);
                        throw new Exception($"FFmpeg failed with exit code {process.ExitCode}. Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

        }



    }

}