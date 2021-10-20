namespace CustomVisionCLI
{
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
    using PowerArgs;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ClassificationTypeTabCompletionSource : SimpleTabCompletionSource
    {
        public ClassificationTypeTabCompletionSource() : base(new string[] { "multiclass", "multilabel" })
        {
            this.MinCharsBeforeCyclingBegins = 0;
        }
    }
    public class DomainTabCompletionSource : SimpleTabCompletionSource
    {
        public DomainTabCompletionSource() : base(new string[] { "General", "General [A1]", "General [A2]", "Food", "Landmarks", "Retail", "Compact domains" })
        {
            this.MinCharsBeforeCyclingBegins = 0;
        }
    }


    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    [TabCompletion(HistoryToSave = 10)]
    [ArgExample("CustomVisionCLI.exe -u {https://[endpoint].cognitiveservices.azure.com} -k {customvisionapikey} -n MyCustomVisionModel -p photos", "using arguments", Title = "Create model using images in directory subfolders")]
    [ArgExample("CustomVisionCLI.exe -u {https://[endpoint].cognitiveservices.azure.com} -k {customvisionapikey} -n MyCustomVisionModel -p testimage.jpg -q", "using arguments", Title = "Custom Vision model quick test using provided image")]
    public class CustomVision
    {
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Endpoint: eg. https://[endpoint].cognitiveservices.azure.com")]
        [ArgShortcut("-u")]
        public string EndpointUri { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Custom Vision API Key")]
        [ArgShortcut("-k")]
        public string CustomVisionAPIKey { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Project Name")]
        [ArgShortcut("-n")]
        public string ProjectName { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Directory path (parent containing subfolders) where images are stored")]
        [ArgShortcut("-p")]
        public string ImagePath { get; set; }

        [DefaultValue(5)]
        [ArgDescription("Timeout in minutes of http request")]
        [ArgShortcut("-t")]
        public int TimeoutMins { get; set; }

        [ArgDescription("Custom Vision model quick test - for use after model has been created & trained")]
        [ArgShortcut("-q")]
        public bool QuickTest { get; set; }

        [ArgumentAwareTabCompletion(typeof(ClassificationTypeTabCompletionSource))]
        [DefaultValue("Multiclass")]
        [ArgDescription("Classification type: multilabel or multiclass (default)")]
        [ArgShortcut("-c")]
        public string ClassificationType { get; set; }

        [ArgumentAwareTabCompletion(typeof(DomainTabCompletionSource))]
        [DefaultValue("General")]
        [ArgDescription("Domain scenario: General, General [A1], General [A2], Food, Landmarks, Retail, Compact domains")]
        [ArgShortcut("-d")]
        public string Domain { get; set; }

        [HelpHook]
        public bool Help { get; set; }

        private CustomVisionTrainingClient _trainingApi = null;
        private Project _project = null;

        public async Task Main()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            _trainingApi = new CustomVisionTrainingClient(new ApiKeyServiceClientCredentials(CustomVisionAPIKey))
            {
                Endpoint = EndpointUri,
            };
            _trainingApi.HttpClient.Timeout = TimeSpan.FromMinutes(TimeoutMins);

            _project = await GetProjectAsync(ProjectName, !QuickTest);

            if (QuickTest) 
            {
                await TestModel();
            }
            else 
            {
                var tags = await CreateTags();
                var uploadCount = await UploadTrainingImages(tags);
                if (tags.Count() > 0 || uploadCount > 0) 
                {
                    await TrainModel();
                }
                else 
                {
                    Console.WriteLine($"No changes made to model");                    
                }
            }

            // fin
            stopwatch.Stop();
            Console.WriteLine($"Done.");
            Console.WriteLine($"Total time: {stopwatch.Elapsed}");
        }

        private async Task<Project> GetProjectAsync(string projectName, bool createIfNotExist = false)
        {
            var projects = await _trainingApi.GetProjectsAsync();
            var project = projects.Where(p => p.Name.Equals(ProjectName)).FirstOrDefault();

            if (project == null && createIfNotExist) 
            {
                Console.WriteLine($"Creating Custom Vision Image Classifer Project: {ProjectName} ({ClassificationType} | {Domain})");

                var domains = await _trainingApi.GetDomainsAsync();
                var selectedDomain = domains.Where(d => d.Name.Equals(Domain)).FirstOrDefault();

                if (selectedDomain == null) 
                {
                    Console.WriteLine($"Domain: {Domain} not found, using General");
                    selectedDomain = domains.Where(d => d.Name.Equals("General")).FirstOrDefault();
                }

                project = await _trainingApi.CreateProjectAsync(name: ProjectName,
                    classificationType: ClassificationType,
                    domainId: selectedDomain.Id);
            }

            return project;
        }

        private async Task<List<Tag>> CreateTags()
        {
            Console.WriteLine($"Scanning subfolders within: {ImagePath}");
            List<Tag> allTags = new List<Tag>();

            var existingTags = await _trainingApi.GetTagsAsync(_project.Id);

            foreach (var folder in Directory.EnumerateDirectories(ImagePath))
            {
                string folderName = Path.GetFileName(folder);
                var tagNames = folderName.Contains(",") ? folderName.Split(',').Select(t => t.Trim()).ToArray() : new string[] { folderName };

                // Create tag for each comma separated value in subfolder name
                foreach (var tag in tagNames)
                {
                    // Check we've not already created this tag from another subfolder
                    if (!allTags.Any(t => t.Name.Equals(tag)))
                    {
                        // Finally check tag not previously created from existing project
                        if (!existingTags.Where(t => t.Name.Equals(tag)).Any())
                        {
                            Console.WriteLine($"Creating Tag: {tag}");
                            var imageTag = await _trainingApi.CreateTagAsync(_project.Id, tag);
                            allTags.Add(imageTag);
                        }
                    }
                }
            }
            return allTags;
        }

        private async Task<int> UploadTrainingImages(List<Tag> allTags)
        {
            int uploadCount = 0;
            foreach (var currentFolder in Directory.EnumerateDirectories(ImagePath))
            {
                string folderName = Path.GetFileName(currentFolder);
                var tagNames = folderName.Contains(",") ? folderName.Split(',').Select(t => t.Trim()).ToArray() : new string[] { folderName };

                try
                {
                    // Load the images to be uploaded from disk into memory
                    Console.WriteLine($"Uploading: {ImagePath}/{folderName} images...");

                    var images = Directory.GetFiles(currentFolder).ToList();
                    var folderTags = allTags.Where(t => tagNames.Contains(t.Name)).Select(t => t.Id).ToList();
                    var imageFiles = images.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img), folderTags)).ToList();
                    var imageBatch = new ImageFileCreateBatch(imageFiles);
                    var summary = await _trainingApi.CreateImagesFromFilesAsync(_project.Id, new ImageFileCreateBatch(imageFiles));

                    // List any images that didn't make it
                    foreach (var imageResult in summary.Images.Where(i => !i.Status.Equals("OK")))
                    {
                        Console.WriteLine($"{ImagePath}/{folderName}/{imageResult.SourceUrl}: {imageResult.Status}");
                    }

                    uploadCount = summary.Images.Where(i => i.Status.Equals("OK")).Count();
                    Console.WriteLine($"Uploaded {uploadCount}/{images.Count()} images successfully from {ImagePath}/{folderName}");
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"Error processing {currentFolder}: {exp.Source}:{exp.Message}");
                }
            }
            return uploadCount;
        }

        private async Task TrainModel()
        {
            try
            {
                // Train CV model and set iteration to the default
                Console.WriteLine($"Training model");
                var iteration = await _trainingApi.TrainProjectAsync(_project.Id);
                while (iteration.Status.Equals("Training"))
                {
                    Thread.Sleep(1000);
                    iteration = await _trainingApi.GetIterationAsync(_project.Id, iteration.Id);
                    Console.WriteLine($"Model status: {iteration.Status}");
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine($"Error training model (check you have at least 5 images per tag and 2 tags)");
                Console.WriteLine($"Error {exp.Source}: {exp.Message}");
            }
        }

        private async Task TestModel()
        {
            // Quick test existing (trained) model
            Console.WriteLine($"Custom Vision Quick test: {ProjectName} with image {ImagePath}");

            if (_project == null)
            {
                Console.WriteLine($"Can't find Custom Vision Project: {ProjectName}");
                return;
            }

            // Read test image
            if (!File.Exists(ImagePath))
            {
                Console.WriteLine($"Can't find image: {ImagePath}");
                return;
            }

            var image = new MemoryStream(await File.ReadAllBytesAsync(ImagePath));

            // Get the default iteration to test against and check results
            var iterations = await _trainingApi.GetIterationsAsync(_project.Id);
            var defaultIteration = iterations.OrderByDescending(i => i.Created).FirstOrDefault();
            if (defaultIteration == null)
            {
                Console.WriteLine($"No default iteration has been set");
                return;
            }

            var result = await _trainingApi.QuickTestImageAsync(_project.Id, image, defaultIteration.Id);
            foreach (var prediction in result.Predictions)
            {
                Console.WriteLine($"Tag: {prediction.TagName} Probability: {prediction.Probability * 100}%");
            }
        }
    }
}
