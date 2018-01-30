namespace CustomVisionCLI
{
    using Microsoft.Cognitive.CustomVision.Training;
    using Microsoft.Cognitive.CustomVision.Training.Models;
    using PowerArgs;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    [TabCompletion(HistoryToSave = 10)]
    [ArgExample("CustomVisionCLI.exe -k {customvisionapikey} -n MyCustomVisionModel -p c:\\photos", "using arguments", Title = "Create model using images in directory subfolders")]
    [ArgExample("CustomVisionCLI.exe -k {customvisionapikey} -n MyCustomVisionModel -p c:\\photos\testimage.jpg -q", "using arguments", Title = "Custom Vision model quick test using provided image")]
    public class CustomVision
    {
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

        [HelpHook]
        public bool Help { get; set; }

        public async Task Main()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            TrainingApi trainingApi = new TrainingApi() { ApiKey = CustomVisionAPIKey };
            trainingApi.HttpClient.Timeout = TimeSpan.FromMinutes(TimeoutMins);

            if (!QuickTest)
            {
                Console.WriteLine($"Creating Custom Vision Project: {ProjectName}");
                var project = trainingApi.CreateProject(ProjectName);

                Console.WriteLine($"Scanning subfolders within: {ImagePath}");
                List<Tag> allTags = new List<Tag>();
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
                            Console.WriteLine($"Creating Tag: {tag}");
                            var imageTag = trainingApi.CreateTag(project.Id, tag);
                            allTags.Add(imageTag);
                        }
                    }
                }

                foreach (var currentFolder in Directory.EnumerateDirectories(ImagePath))
                {
                    string folderName = Path.GetFileName(currentFolder);
                    var tagNames = folderName.Contains(",") ? folderName.Split(',').Select(t => t.Trim()).ToArray() : new string[] { folderName };

                    try
                    {
                        // Load the images to be uploaded from disk into memory
                        Console.WriteLine($"Uploading: {ImagePath}\\{folderName} images...");

                        var images = Directory.GetFiles(currentFolder).ToList();
                        var folderTags = allTags.Where(t => tagNames.Contains(t.Name)).Select(t => t.Id).ToList();
                        var imageFiles = images.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img), folderTags)).ToList();
                        var imageBatch = new ImageFileCreateBatch(imageFiles);
                        var summary = await trainingApi.CreateImagesFromFilesAsync(project.Id, new ImageFileCreateBatch(imageFiles));

                        // List any images that didn't make it
                        foreach (var imageResult in summary.Images.Where(i => !i.Status.Equals("OK")))
                        {
                            Console.WriteLine($"{ImagePath}\\{folderName}\\{imageResult.SourceUrl}: {imageResult.Status}");
                        }

                        Console.WriteLine($"Uploaded {summary.Images.Where(i => i.Status.Equals("OK")).Count()}/{images.Count()} images successfully from {ImagePath}\\{folderName}");

                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine($"Error processing {currentFolder}: {exp.Source}:{exp.Message}");
                    }
                }

                try
                {
                    // Train CV model and set iteration to the default
                    Console.WriteLine($"Training model");
                    var iteration = trainingApi.TrainProject(project.Id);
                    while (iteration.Status.Equals("Training"))
                    {
                        Thread.Sleep(1000);
                        iteration = trainingApi.GetIteration(project.Id, iteration.Id);
                        Console.WriteLine($"Model status: {iteration.Status}");
                    }

                    if (iteration.Status.Equals("Completed"))
                    {
                        iteration.IsDefault = true;
                        trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);
                        Console.WriteLine($"Iteration: {iteration.Id} set as default");
                    }
                    else
                    {
                        Console.WriteLine($"Iteration status: {iteration.Status}");
                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"Error training model (check you have at least 5 images per tag and 2 tags)");
                    Console.WriteLine($"Error {exp.Source}: {exp.Message}");
                }
            }
            else
            {
                // Quick test existing (trained) model
                Console.WriteLine($"Custom Vision Quick test: {ProjectName} with image {ImagePath}");

                // Retrieve CV project
                var projects = trainingApi.GetProjects();
                var project = projects.Where(p => p.Name.Equals(ProjectName)).FirstOrDefault();
                if (project == null)
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

                var image = new MemoryStream(File.ReadAllBytes(ImagePath));

                // Get the default iteration to test against and check results
                var iterations = trainingApi.GetIterations(project.Id);
                var defaultIteration = iterations.Where(i => i.IsDefault == true).FirstOrDefault();
                if (defaultIteration == null)
                {
                    Console.WriteLine($"No default iteration has been set");
                    return;
                }

                var result = trainingApi.QuickTestImage(project.Id, image, defaultIteration.Id);
                foreach (var prediction in result.Predictions)
                {
                    Console.WriteLine($"Tag: {prediction.Tag} Probability: {prediction.Probability}");
                }
            }

            // fin
            stopwatch.Stop();
            Console.WriteLine($"Done.");
            Console.WriteLine($"Total time: {stopwatch.Elapsed}");
        }
    }
}
