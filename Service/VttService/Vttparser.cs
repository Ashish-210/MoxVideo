using System.Globalization;

public class VttParserService
{
    public async Task<List<TextChunk>> ParseVttFileAsync(string filePath)
    {
        var segments = new List<TextChunk>();
        TextChunk currentSegment = null;
        bool readingText = false;

        // Read all lines from the VTT file
        var allLines = await File.ReadAllLinesAsync(filePath);

        foreach (var line in allLines)
        {
            // Skip the WEBVTT header and empty lines that are not part of a cue's text
            if (line.Equals("WEBVTT", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(line) || line.StartsWith("LanguageCode :"))
            {
                readingText = false;
                continue;
            }

            // Check if the line is a timestamp line (e.g., "00:00:00.419 --> 00:00:03.839")
            if (IsTimestampLine(line))
            {
                // If we were already building a segment, add it to the list before starting a new one
                if (currentSegment != null)
                {
                    segments.Add(currentSegment);
                }

                readingText = false;
                currentSegment = new TextChunk();

                // Try to parse the timestamp line
                ParseTimestampLine(line, currentSegment);
                continue;
            }

            // If the current line is not a timestamp and we have a current segment,
            // then this line is part of the subtitle text.
            if (currentSegment != null)
            {
                // If we've already started reading text for this segment, append a newline first
                if (readingText)
                {
                    currentSegment.Text += Environment.NewLine;
                }
                else
                {
                    readingText = true;
                }

                currentSegment.Text += line;
            }
        }

        // Don't forget to add the last segment after the loop ends!
        if (currentSegment != null)
        {
            segments.Add(currentSegment);
        }

        return segments;
    }
    public async Task<string> GetSourceLang(string vttfile)
    {
        var allLines = await File.ReadAllLinesAsync(vttfile);
        foreach (var line in allLines)
        {
            if (line.StartsWith("LanguageCode :"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    var lang = parts[1].Trim();
                    return lang;
                }
                break; // Exit the loop after finding the language line
            }
        }
        return "en"; // Default to English if no language line is found 

    }
    private bool IsTimestampLine(string line)
    {
        // A timestamp line must contain the arrow "-->"
        return line.Contains("-->");
    }
    private void ParseTimestampLine(string line, TextChunk segment)
    {
        // Example line: "00:00:00.419 --> 00:00:03.839"
        // Or with sequence number: "1\n00:00:00.419 --> 00:00:03.839"

        // Split the line by spaces and remove empty entries
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // The first part should be the start time
        if (parts.Length > 0 && TimeSpan.TryParse(parts[0], CultureInfo.InvariantCulture, out var startTime))
        {
            segment.StartTime = startTime.TotalSeconds; // Convert TimeSpan to double (seconds)
        }

        // The third part (after the "-->") should be the end time
        if (parts.Length > 2 && TimeSpan.TryParse(parts[2], CultureInfo.InvariantCulture, out var endTime))
        {
            segment.EndTime = endTime.TotalSeconds; // Convert TimeSpan to double (seconds)
        }

        // Note: We ignore the sequence number if it's on its own line.
        // It will be captured in the previous loop iteration and is often optional.
    }
}