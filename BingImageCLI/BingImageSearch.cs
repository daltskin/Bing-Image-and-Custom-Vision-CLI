namespace BingImageCLI
{
    using Newtonsoft.Json;
    using PowerArgs;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class LicenseTabCompletionSource : SimpleTabCompletionSource
    {
        public LicenseTabCompletionSource() : base(new string[] { "Any", "Public", "Share", "ShareCommercially", "Modify", "ModifyCommercially", "All" })
        {
            this.MinCharsBeforeCyclingBegins = 0;
        }
    }

    public class SafeSearchTabCompletionSource : SimpleTabCompletionSource
    {
        public SafeSearchTabCompletionSource() : base(new string[] { "Off", "Moderate", "Strict" })
        {
            this.MinCharsBeforeCyclingBegins = 0;
        }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    [TabCompletion(HistoryToSave = 10)]
    [ArgExample("BingImageSearch.exe -u https://api.cognitive.microsoft.com/bing/v7.0/images/search -k {yourbingapikey} -s Clippy -p c:\\photos -m 30 -l Public -ss Off", "using safe search filter", Title = "Override base uri, return max 30 results, public license images only, may return images with adult content")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -s Clippy -p c:\\photos -m 30 -l Public -ss Off", "using safe search filter", Title = "Return max 30 results, public license images only, may return images with adult content")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -s Clippy -p c:\\photos -m 30 -l Public", "using license filter", Title = "Return max 30 results, public license images only")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -s Clippy -p c:\\photos -m 30", "using count filter", Title = "Return max 30 results")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -s Clippy -p c:\\photos", "using arguments", Title = "Simple query")]
    public class BingImageSearch
    {
        [DefaultValue("https://api.cognitive.microsoft.com/bing/v7.0/images/search")]
        [ArgDescription("Bing Uri Base: eg. https://api.cognitive.microsoft.com/bing/v7.0/images/search")]
        [ArgShortcut("-u")]
        public string BingUri { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Bing API Key")]
        [ArgShortcut("-k")]
        public string BingAPIKey { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Bing search term/query")]
        [ArgShortcut("-s")]
        public string SearchTerm { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Directory path to save results to")]
        [ArgShortcut("-p")]
        public string DestinationPath { get; set; }

        // https://docs.microsoft.com/en-gb/rest/api/cognitiveservices/bing-images-api-v7-reference
        [DefaultValue(35)]
        [ArgDescription("Maximum number of results to return")]
        [ArgRange(1, 150)]
        [ArgShortcut("-m")]
        public int MaxResultCount { get; set; }

        [ArgumentAwareTabCompletion(typeof(SafeSearchTabCompletionSource))]
        [DefaultValue("Strict")]
        [ArgDescription("Safe Search Filter")]
        [ArgShortcut("-ss")]
        public string SafeSearch { get; set; }

        [ArgumentAwareTabCompletion(typeof(LicenseTabCompletionSource))]
        [ArgRequired(PromptIfMissing = true)]
        [ArgShortcut("-l")]
        public string License { get; set; }

        [HelpHook]
        public bool Help { get; set; }

        public async Task Main()
        {
            Console.WriteLine("Searching Bing images for: " + SearchTerm);
            var result = await GetBingSearchResult(BingUri, BingAPIKey, SearchTerm, MaxResultCount, SafeSearch, License);
            if (result != null)
            {
                Console.WriteLine($"{result?.value.Length ?? 0} images found");
                string path = $"{DestinationPath}\\{SearchTerm}";

                var savedImageCount = await SaveBingSearchImages(result, path);
                Console.WriteLine($"{savedImageCount} images saved");
            }
        }

        private async Task<BingSearchResult> GetBingSearchResult(string uriBase, string apiKey, string searchQuery, int maxResultCount, string safeSearch, string license)
        {
            HttpClient httpClient = new HttpClient();

            var uriQuery = $"{uriBase}?q={Uri.EscapeDataString(searchQuery)}&count={maxResultCount}&safesearch={safeSearch}&license={license}";
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, uriQuery);
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            try
            {
                using (HttpResponseMessage response = await httpClient.SendAsync(httpRequest))
                {
                    response.EnsureSuccessStatusCode();
                    string resp = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<BingSearchResult>(resp);
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine($"Error calling Bing Search API (check your key): {exp.Message}");
                return null;
            }
        }

        private async Task<int> SaveBingSearchImages(BingSearchResult results, string targetFolder)
        {
            Directory.CreateDirectory(targetFolder);
            HttpClient httpClient = new HttpClient();

            List<Task> taskList = new List<Task>();
            int savedCount = 0;
            foreach (var item in results.value)
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var httpRequest = new HttpRequestMessage(HttpMethod.Get, item.contentUrl);
                        var response = await httpClient.SendAsync(httpRequest);
                        response.EnsureSuccessStatusCode();
                        var stream = await response.Content.ReadAsStreamAsync();

                        var img = Image.FromStream(stream);
                        string filePath = $"{targetFolder}\\{item.imageId}.jpg";
                        img.Save(filePath);
                        savedCount++;
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine($"Error downloading: {item.contentUrl} Error: {exp.Message}");
                    }
                });
                taskList.Add(task);
            }
            await Task.WhenAll(taskList.ToArray());
            return savedCount;
        }
    }
}
