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

        public void Main()
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

                Parallel.ForEach(Directory.EnumerateDirectories(ImagePath), (currentFolder) =>
                {
                    string folderName = Path.GetFileName(currentFolder);
                    var tagNames = folderName.Contains(",") ? folderName.Replace(" ", "").Split(',') : new string[] { folderName };

                    try
                    {
                        // Load the images to be uploaded from disk into memory
                        Console.WriteLine($"Uploading: {ImagePath}\\{folderName} images...");
                        var images = Directory.GetFiles(currentFolder).Select(f => new MemoryStream(File.ReadAllBytes(f))).ToList();

                        // More reliable to upload individually than via batch for some reason
                        Parallel.ForEach(images, (image) =>
                        {
                            trainingApi.CreateImagesFromData(project.Id, image, allTags.Where(t => tagNames.Contains(t.Name)).Select(t => t.Id.ToString()).ToList());
                        });
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine($"Error uploading: {exp.Message}");
                    }
                });

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

                // Get the default iteration to test against and check results
                var iterations = trainingApi.GetIterations(project.Id);
                var image = new MemoryStream(File.ReadAllBytes(ImagePath));
                var result = trainingApi.QuickTestImage(project.Id, image, iterations.Where(i=> i.IsDefault == true).FirstOrDefault().Id);
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
