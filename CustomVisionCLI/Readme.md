# Custom Vision Model CLI - with image uploading, tagging and training
Cross platform CLI to provision a new Microsoft Custom Vision model using images stored on your local machine.  Images are automatically uploaded and tagged using folder names and the model is trained ready for predications.

To learn more about Microsoft Cognitive Custom Vision Service, please see here: https://azure.microsoft.com/en-gb/services/cognitive-services/custom-vision-service/

## Custom Vision API Key
To retrieve your Custom Vision API key start here: https://azure.microsoft.com/en-gb/try/cognitive-services/

## CLI Arguments

| Argument name | shortcut | example |
|----|----|----|
| CustomVisionAPIKey | -k | asdfasdfasdfsaf |
| ProjectName | -n | PaperclipOrClippy | 
| ImagePath | -p | c:\photos |
| Timeout (minutes) | -t | 10 |
| QuickTest | -q | |

## Usage

### Model creation, image upload & training
Upload all subfolders of images under the c:\\photos\ path.  Folder names are comma separated to specify multiple tags eg:
"c:\photos\cucumber,vegetable" will upload all images in the folder: "c:\photos\cucumber,vegetable" and tag them with both "cucumber" and "vegetable" tags.
```
CustomVisionCLI.exe -k *yourcustomvisionapikey* -n CucumberOrCourgette -p c:\photos
```

### Model quick test
Quickly test your model with a single image to see the outcome prediction

```
CustomVisionCLI.exe -k *yourcustomvisionapikey* -n CucumberOrCourgette -p c:\photos\unseen\cucumber.jpg -q
```





