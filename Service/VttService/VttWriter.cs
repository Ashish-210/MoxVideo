using SubtitlesParser.Classes;
using SubtitlesParser.Classes.Writers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace MoxVideo.Service.VttService
{
    public class VttWriter : ISubtitlesWriter
    {
        private IEnumerable<string> SubtitleItemToSubtitleEntry(SubtitleItem subtitleItem, int subtitleEntryNumber, bool includeFormatting)
        {
            // take the start and end timestamps and format it as a timecode line
            string formatTimecodeLine()
            {
                TimeSpan start = TimeSpan.FromMilliseconds(subtitleItem.StartTime);
                TimeSpan end = TimeSpan.FromMilliseconds(subtitleItem.EndTime);
                return $"{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}";
            }

            List<string> lines = new List<string>();
            //lines.Add(subtitleEntryNumber.ToString());
            lines.Add(formatTimecodeLine());
            // check if we should be including formatting or not (default to use formatting if plaintextlines isn't set) 
            //if (includeFormatting == false && subtitleItem.PlaintextLines != null)
            //    lines.AddRange(subtitleItem.PlaintextLines);
            //else
                lines.AddRange(subtitleItem.Lines);

            return lines;
        }
        public void WriteFile(string file, IEnumerable<SubtitleItem> subtitleItems, bool includeFormatting = true)
        {
        using (FileStream _fileStream = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                       
                            WriteStream(_fileStream, subtitleItems, includeFormatting);
  
        }
        public void WriteStream(Stream stream, IEnumerable<SubtitleItem> subtitleItems, bool includeFormatting = true)
        {
            using TextWriter writer = new StreamWriter(stream);

            List<SubtitleItem> items = subtitleItems.ToList(); // avoid multiple enumeration since we're using a for instead of foreach
            for (int i = 0; i < items.Count; i++)
            {
                SubtitleItem subtitleItem = items[i];
                IEnumerable<string> lines = SubtitleItemToSubtitleEntry(subtitleItem, i + 1, includeFormatting); // add one because subtitle entry numbers start at 1 instead of 0
                foreach (string line in lines)
                    writer.WriteLine(line);

                writer.WriteLine(); // empty line between subtitle entries
            }
        }

        public async Task WriteStreamAsync(Stream stream, IEnumerable<SubtitleItem> subtitleItems, bool includeFormatting = true)
        {
            await using TextWriter writer = new StreamWriter(stream);

            List<SubtitleItem> items = subtitleItems.ToList(); // avoid multiple enumeration since we're using a for instead of foreach
            for (int i = 0; i < items.Count; i++)
            {
                SubtitleItem subtitleItem = items[i];
                IEnumerable<string> lines = SubtitleItemToSubtitleEntry(subtitleItem, i + 1, includeFormatting); // add one because subtitle entry numbers start at 1 instead of 0
                foreach (string line in lines)
                    await writer.WriteLineAsync(line);

                await writer.WriteLineAsync(); // empty line between subtitle entries
            }
        }
    }
}
