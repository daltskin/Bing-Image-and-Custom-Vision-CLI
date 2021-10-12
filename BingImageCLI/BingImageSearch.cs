namespace BingImageCLI
{
    using PowerArgs;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Web;

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

    public class SizeTabCompletionSource : SimpleTabCompletionSource
    {
        public SizeTabCompletionSource() : base(new string[] { "Small", "Medium", "Large", "Wallpaper", "All" })
        {
            this.MinCharsBeforeCyclingBegins = 0;
        }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    [TabCompletion(HistoryToSave = 10)]
    [ArgExample("BingImageSearch.exe -u https://api.bing.microsoft.com/v7.0/images/search -k {yourbingapikey} -q Clippy -m 30 -l Public -s Large", "using safe search filter", Title = "Override base uri, return max 30 results, public license images only, safe search, Large (500x500) or bigger images")]
    [ArgExample("BingImageSearch.exe -u https://api.bing.microsoft.com/v7.0/images/search -k {yourbingapikey} -q Clippy -m 30 -l Public -ss Off", "using safe search filter", Title = "Override base uri, return max 30 results, public license images only, may return images with adult content")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -q Clippy -m 30 -l Public -ss Off", "using safe search filter", Title = "Return max 30 results, public license images only, may return images with adult content")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -q Clippy -m 30 -l Public", "using license filter", Title = "Return max 30 results, public license images only")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -q Clippy -m 30", "using count filter", Title = "Return max 30 results")]
    [ArgExample("BingImageSearch.exe -k {yourbingapikey} -q Clippy", "using arguments", Title = "Simple query")]
    public class BingImageSearch
    {
        [DefaultValue("https://api.bing.microsoft.com/v7.0/images/search")]
        [ArgDescription("Bing Uri Base: eg. https://api.bing.microsoft.com/v7.0/images/search")]
        [ArgShortcut("-u")]
        public string BingUri { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Bing API Key")]
        [ArgShortcut("-k")]
        public string BingAPIKey { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Bing search term/query")]
        [ArgShortcut("-q")]
        public string SearchTerm { get; set; }

        // https://docs.microsoft.com/en-us/rest/api/cognitiveservices-bingsearch/bing-images-api-v7-reference
        [DefaultValue(35)]
        [ArgDescription("Maximum number of results to return")]
        [ArgRange(1, 150)]
        [ArgShortcut("-m")]
        public int MaxResultCount { get; set; }

        [ArgumentAwareTabCompletion(typeof(SizeTabCompletionSource))]
        [DefaultValue("All")]
        [ArgDescription("Image size")]
        [ArgShortcut("-s")]
        public string ImageSize { get; set; }

        [ArgumentAwareTabCompletion(typeof(SafeSearchTabCompletionSource))]
        [DefaultValue("Moderate")]
        [ArgDescription("Safe Search Filter")]
        [ArgShortcut("-ss")]
        public string SafeSearch { get; set; }

        [ArgumentAwareTabCompletion(typeof(LicenseTabCompletionSource))]
        [DefaultValue("All")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgShortcut("-l")]
        public string License { get; set; }

        [HelpHook]
        public bool Help { get; set; }

        public async Task Main()
        {
            Console.WriteLine("Searching Bing images for: " + SearchTerm);

            var httpClient = new HttpClient();

            var url = $"{BingUri}?q={HttpUtility.UrlEncode(SearchTerm)}" +
                        $"&count={MaxResultCount}" +
                        $"&safeSearch={SafeSearch}" +
                        $"&license={License}" +
                        $"&size={ImageSize}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", BingAPIKey);
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var bingResponse = JsonSerializer.Deserialize<BingResponse>(content);

            if (bingResponse.value != null)
            {
                string path = $"{Directory.GetCurrentDirectory()}/{SearchTerm}";
                var (savedImageCount, errorCount) = await SaveBingSearchImages(bingResponse, path);
                Console.WriteLine($"{savedImageCount} images saved ({errorCount} errors)");
            }
        }

        private async Task<(int, int)> SaveBingSearchImages(BingResponse results, string targetFolder)
        {
            Directory.CreateDirectory(targetFolder);
            HttpClient httpClient = new HttpClient();

            List<Task> taskList = new List<Task>();
            int savedCount = 0;
            int errorCount = 0;
            foreach (var item in results.value)
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var httpRequest = new HttpRequestMessage(HttpMethod.Get, item.contentUrl);
                        var response = await httpClient.SendAsync(httpRequest);
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsByteArrayAsync();
                        string filePath = $"{targetFolder}/{item.imageId}.{item.encodingFormat}";
                        await File.WriteAllBytesAsync(filePath, content);
                        savedCount++;
                    }
                    catch (Exception)
                    {
                        errorCount++;
                        Console.WriteLine($"Error downloading: {item.contentUrl}");
                    }
                });
                taskList.Add(task);
            }
            await Task.WhenAll(taskList.ToArray());
            return (savedCount, errorCount);
        }
    }
}
