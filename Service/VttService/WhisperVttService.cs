using MoxVideo.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Whisper.net;

public class WhisperVttService
{
    private readonly string _modelPath;
    private readonly HttpClient _httpClient;
   
    public WhisperVttService()
    {
        _modelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                 ".whisper", "ggml-base.bin");
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Creates VTT file using Whisper.NET (recommended approach)
    /// </summary>
    public async Task<string> CreateVttWithWhisperNet(string audioFilePath, string outputVttPath, string language = "en")
    {
        string workingAudioFile = audioFilePath;
        bool isTemporaryFile = false;

        // Convert MP3 to WAV if needed
        if (Path.GetExtension(audioFilePath).ToLower() == ".mp3")
        {
            var tempWavFile = Path.GetTempFileName() + ".wav";
            workingAudioFile = await CreateVttWithPythonWhisper(audioFilePath, tempWavFile);
            isTemporaryFile = true;
        }
        // Ensure model is downloaded
        await EnsureModelDownloaded();

        using var whisperFactory = WhisperFactory.FromPath(_modelPath);
        using var processor = whisperFactory.CreateBuilder()
            .WithLanguage(language)
            .Build();

        var vttContent = new StringBuilder();
        vttContent.AppendLine("WEBVTT");
        vttContent.AppendLine();

        int segmentIndex = 1;

        // Open the audio file as a stream
        await using var audioFileStream = File.OpenRead(audioFilePath);

        // Pass the stream to ProcessAsync
        await foreach (var result in processor.ProcessAsync(audioFileStream))
        {
            var startTime = TimeSpan.FromMilliseconds(result.Start.TotalMilliseconds);
            // Fix for CS1503: Convert TimeSpan to double (milliseconds) before passing to TimeSpan.FromMilliseconds
            var endTime = TimeSpan.FromMilliseconds(result.End.TotalMilliseconds);

            vttContent.AppendLine($"{segmentIndex}");
            vttContent.AppendLine($"{FormatTime(startTime)} --> {FormatTime(endTime)}");
            vttContent.AppendLine(result.Text.Trim());
            vttContent.AppendLine();

            segmentIndex++;
        }

        await File.WriteAllTextAsync(outputVttPath, vttContent.ToString());
        return outputVttPath;
    }

    /// <summary>
    /// Creates VTT file by calling Python Whisper executable
    /// </summary>
    public async Task<string> CreateVttWithPythonWhisper(string audioFilePath, string outputVttPath, string language = "en")
    {
        var outputDir = Path.GetDirectoryName(outputVttPath);
        var fileName = Path.GetFileNameWithoutExtension(audioFilePath);

        ProcessStartInfo startInfo = new()
        {
            FileName = "whisper",
            Arguments = $"\"{audioFilePath}\" --model base --output_format vtt --output_dir \"{outputDir}\" --language {language}",
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var generatedVttPath = Path.Combine(outputDir, $"{fileName}.vtt");
                if (generatedVttPath != outputVttPath && File.Exists(generatedVttPath))
                {
                    File.Move(generatedVttPath, outputVttPath);
                }
                return outputVttPath;
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Whisper failed: {error}");
            }
        }

        throw new Exception("Failed to start Whisper process");
    }

    /// <summary>
    /// Creates VTT file from JSON transcript data (your existing format)
    /// </summary>
    public async Task<string> CreateVttFromJsonTranscript(SpeechToTextResponse transcriptData, string outputVttPath)
    {
        try
        {
            //var transcriptData = JsonSerializer.Deserialize<TranscriptData>(jsonData);

            var vtt = new StringBuilder();
            vtt.AppendLine("WEBVTT");
            vtt.AppendLine();
            vtt.AppendLine("LanguageCode :" + transcriptData.LanguageCode);

            var chunks = CreateTextChunks(transcriptData.Words, maxWordsPerChunk: 12, maxDurationSeconds: 5.0);

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
               
                vtt.AppendLine($"{FormatTime(TimeSpan.FromSeconds(chunk.StartTime))} --> {FormatTime(TimeSpan.FromSeconds(chunk.EndTime))}");
                vtt.AppendLine(chunk.Text.Trim());
                vtt.AppendLine();
            }

            await File.WriteAllTextAsync(outputVttPath, vtt.ToString());
            return outputVttPath;
        }catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return "Error in vtt formation";
        }
    }
    public async Task<string> CreateVttfromSegements(List<TextChunk> segments, string outputVttPath)
    {
        try
        {
            var vttContentBuilder = new StringBuilder();
            vttContentBuilder.AppendLine("WEBVTT");
            vttContentBuilder.AppendLine();
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                // Convert StartTime and EndTime (double, in seconds) to TimeSpan before formatting
                var start = TimeSpan.FromSeconds(segment.StartTime);
                var end = TimeSpan.FromSeconds(segment.EndTime);
                vttContentBuilder.AppendLine($"{start:hh\\:mm\\:ss\\.fff} --> {end:hh\\:mm\\:ss\\.fff}");

                vttContentBuilder.AppendLine(segment.Text);

                if (i < segments.Count - 1)
                {
                    vttContentBuilder.AppendLine();
                }
            }

            await File.WriteAllTextAsync(outputVttPath, vttContentBuilder.ToString());
            return outputVttPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return "Error in vtt formation";
        }
    }
    public string findlangcode(string language)
    {
        Dictionary<string, string> LanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)

        {
            // English
            ["en"] = "en",
            ["eng"] = "en",
            ["english"] = "en",
            ["English"] = "en",
            ["en-IN"]="English",

            // Hindi
            ["hi"] = "hi",
            ["hin"] = "hi",
            ["hindi"] = "hi",
            ["Hindi"] = "hi",

            // Spanish
            ["es"] = "es",
            ["spa"] = "es",
            ["spanish"] = "es",
            ["Spanish"] = "es",

            // Tamil
            ["ta"] = "ta",
            ["tam"] = "ta",
            ["tamil"] = "ta",
            ["Tamil"] = "ta",

            // Telugu
            ["te"] = "te",
            ["tel"] = "te",
            ["telugu"] = "te",
            ["Telugu"] = "te",

            // Add all other languages you support...
            // French
            ["fr"] = "fr",
            ["fra"] = "fr",
            ["french"] = "fr",
            ["French"] = "fr",

            // German
            ["de"] = "de",
            ["deu"] = "de",
            ["german"] = "de",
            ["German"] = "de",

            // Japanese
            ["ja"] = "ja",
            ["jpn"] = "ja",
            ["japanese"] = "ja",
            ["Japanese"] = "ja"
        };
        if (LanguageMap.TryGetValue(language, out var name))
        {
            return name;
        }
        // Fallback: return the code itself if no name is found
        return language;
    }

    private async Task EnsureModelDownloaded(string modelName = "base")
    {
        if (!File.Exists(_modelPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_modelPath));

            var modelUrl = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-{modelName}.bin";

            Console.WriteLine($"Downloading Whisper {modelName} model...");
            var response = await _httpClient.GetAsync(modelUrl);
            response.EnsureSuccessStatusCode();

            await using var fileStream = File.Create(_modelPath);
            await response.Content.CopyToAsync(fileStream);

            Console.WriteLine("Model downloaded successfully.");
        }
    }

    private List<TextChunk> CreateTextChunks(Word[] words, int maxWordsPerChunk = 12, double maxDurationSeconds = 5.0)
    {
        var chunks = new List<TextChunk>();
        var currentChunk = new List<Word>();

        foreach (var word in words.Where(w => w.Type == "word"))
        {
            currentChunk.Add(word);

            bool shouldBreak = word.Text.EndsWith(".") || word.Text.EndsWith("!") || word.Text.EndsWith("?");

            if (shouldBreak)
            {
                if (currentChunk.Count > 0)
                {
                    chunks.Add(new TextChunk
                    {
                        StartTime = currentChunk[0].Start,
                        EndTime = currentChunk.Last().End,
                        Text = string.Join(" ", currentChunk.Select(w => w.Text))
                    });
                }
                currentChunk.Clear();
            }
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(new TextChunk
            {
                StartTime = currentChunk[0].Start,
                EndTime = currentChunk.Last().End,
                Text = string.Join(" ", currentChunk.Select(w => w.Text))
            });
        }

        return chunks;
    }

    private string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Supporting classes
public class TranscriptData
{
    public string language_code { get; set; }
    public double language_probability { get; set; }
    public string text { get; set; }
    public List<WordData> words { get; set; }
}

public class WordData
{
    public string text { get; set; }
    public double start { get; set; }
    public double end { get; set; }
    public string type { get; set; }
    public string speaker_id { get; set; }
    public double logprob { get; set; }
}

public class TextChunk
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; }
}
