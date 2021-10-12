# Bing Image CLI - Bing Image Search API Results - Image Downloader
Cross platform CLI to download images from Bing to your local machine using Microsoft Cognitive Services - Bing Image Search.  

For more details on usage see [Bing Image CLI](BingImageCLI/Readme.md)

# Custom Vision Model CLI - with image uploading, tagging and training
Cross platform CLI to provision a new Microsoft Custom Vision model using images stored on your local machine. 

For more details on usage see [Custom Vision CLI](CustomVisionCLI/Readme.md)

# Why?
Often to create your own classifer you need a set of images to start from, if you have some already then great - you can jump straight to the Custom Vision Model CLI.  If not, you can use Bing to source them for you.  Using Bing Image CLI you can download the images directly to your local machine, then add or remove any other images to fine tune your image stock.  Then you can use the Custom Vision Model CLI to create a new model, upload the images and correctly label each one.  Further more, the model is trained and then made available for running quick tests - to see if you're classifer is returning the expected results.

# Demo
Create a Custom Vision classifer in 30 seconds starting from nothing.  Using both of these CLI's together, you can quickly experiment with Custom Vision for anything.  Here, in this demo, the goal is to create a Custom Vision Model that can determine images of cucumbers from courgettes.  I start from scratch, first downloading images from Bing using the Bing API of courgettes and then cucumbers.  These are stored in local folders, using the search term as the folder name (comma separated - which can be tweaked after if required).  Then, a Custom Vision model is created named "CucumberOrCourgette", and the parent folder of all the images is provided.  Within the Custom Vision model, labels are created for each search term that was used (cucumber, courgette, green, vegetable).  As each folder of images is uploaded, the appropriate tags are added to each image.  The Custom Vision model is then trained and the default iteration is set. Finally, the model is tested by providing an unseen image to classify.

![Demo](Images/Bing%20Image%20and%20Custom%20Vision%20CLI.gif)









