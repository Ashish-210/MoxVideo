namespace MoxVideo.Service
{

    public class LanguageCodeHelper
    {
        private readonly Dictionary<string, List<string>> LanguageMap = new()
        {
            // English
            { "English", new List<string> { "en", "eng" } },
            
            // South Asian Languages
            { "Bengali", new List<string> { "bn", "ben" } },
            { "Gujarati", new List<string> { "gu", "guj" } },
            { "Hindi", new List<string> { "hi", "hin" } },
            { "Kannada", new List<string> { "kn", "kan" } },
            { "Malayalam", new List<string> { "ml", "mal" } },
            { "Marathi", new List<string> { "mr", "mar" } },
            { "Oriya", new List<string> { "or", "ori" } },
            { "Punjabi", new List<string> { "pa", "pan" } },
            { "Tamil", new List<string> { "ta", "tam" } },
            { "Telugu", new List<string> { "te", "tel" } },
            { "Urdu", new List<string> { "ur", "urd" } },
            
            // Middle Eastern Languages
            { "Arabic", new List<string> { "ar", "ara" } },
            
            // European Languages
            { "French", new List<string> { "fr", "fra" } },
            { "German", new List<string> { "de", "deu" } },
            { "Spanish", new List<string> { "es", "spa" } },
        };
        public LanguageCodeHelper()
        {

        }

        public  string GetLanguageCode(string languageName)
        {
            if (string.IsNullOrWhiteSpace(languageName))
                return "en"; // default fallback

            return LanguageMap.TryGetValue(languageName, out var codes) && codes.Count > 0
                ? codes[0]
                : "en";
        }

        public  string GetLanguageName(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return "English"; // default fallback

            var match = LanguageMap.FirstOrDefault(x => x.Value.Contains(languageCode));
            return !string.IsNullOrEmpty(match.Key) ? match.Key : "English";
        }

    }


}
