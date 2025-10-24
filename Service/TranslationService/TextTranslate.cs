using Microsoft.Extensions.Options;
using MoxVideo.Service.HelperService;

namespace MoxVideo.Service
{
    public class Data
    {
        public string text { get; set; }
        public string qual { get; set; }
        public string op { get; set; }
        public string recid { get; set; }
    }
    [Serializable]
    public class Record
    {

        public string key { get; set; }
        public List<string> lang { get; set; }
     
        public List<Data> Data { get; set; }
        public string InputLanguage { get; set; } = "English";
    }
    [Serializable]
    public class RespData
    {
        public List<Output> data { get; set; }
    }
    [Serializable]
    public class Output
    {
        public string field { get; set; }
        public string text { get; set; }
        public string orgtext { get; set; }
        public int qual { get; set; }
        public string lang { get; set; }
        public string recid { get; set; }
        public string error { get; set; }
    }
    public class TextTranslate
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<KeySetting> _appsetting;

        public TextTranslate(IHttpClientFactory factory, IOptions<KeySetting> AppSetting)
        {
            _httpClient = factory.CreateClient("MoxWave");
            _appsetting = AppSetting;
        }

        public async Task<string> Translate(string inlanguage, string outlanguage, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Record record = new Record();
                record.InputLanguage = inlanguage;
                record.key = _appsetting.Value.WaveKey;
                record.lang = new List<string> { outlanguage };
              
                record.Data = new List<Data>
                {
                    new Data
                    {
                        op = "0",
                        text = text,
                        qual = "6"
                    }
                };
                string postJson = Newtonsoft.Json.JsonConvert.SerializeObject(record);
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/p9/Moxapi.ashx");
                httpRequestMessage.Content = new StringContent(postJson, System.Text.Encoding.UTF8, "application/json");
                var response = _httpClient.SendAsync(httpRequestMessage).Result;
                if (response.IsSuccessStatusCode)
                {
                    RespData? respJson = await response.Content.ReadFromJsonAsync<RespData>();
                    if (respJson?.data != null && respJson.data.Count > 0)
                    {
                        return respJson.data[0].text;
                    }
                }
                return string.Empty;
            }
            return string.Empty;
        }
    }
}
