# Custom Vision Image Classifer CLI
Cross platform CLI to provision a new Microsoft Custom Vision Image Classifer model using images stored on your local machine.  Images are automatically uploaded and tagged using folder names.  The model is trained and then ready for predications.

To learn more about Microsoft Cognitive Custom Vision Service, please see here: https://azure.microsoft.com/en-gb/services/cognitive-services/custom-vision-service/

## Custom Vision API Key
To retrieve your Custom Vision API key start here: https://azure.microsoft.com/en-gb/try/cognitive-services/

## CLI Arguments

| Argument name | shortcut | example |
|----|----|----|
| Endpoint | -u | https://[endpoint].cognitiveservices.azure.com | 
| CustomVisionAPIKey | -k | |
| ProjectName | -n | PaperclipOrClippy | 
| ImagePath | -p | photos |
| Timeout (minutes) | -t | 10 |
| ClassifierType | -c | multiclass (single tag per image) or multilabel (multiple tags per image) |
| Domain | -d | https://docs.microsoft.com/en-us/azure/cognitive-services/custom-vision-service/select-domain |
| QuickTest | -q | |

## Usage

### Model creation, image upload & training
Upload all subfolders of images under the relative parent photos path.  Folder names are comma separated to specify multiple tags eg:
"photos/cucumber,vegetable" will upload all images in the folder: "/photos/cucumber,vegetable" and tag them with both "cucumber" and "vegetable" tags.
```
CustomVisionCLI.exe -u https://[endpoint].cognitiveservices.azure.com -k *yourcustomvisionapikey* -n CucumberOrCourgette -p photos -c multiclass
```

### Model quick test
Quickly test your model with a single image to see the outcome prediction

```
CustomVisionCLI.exe -u https://[endpoint].cognitiveservices.azure.com -k *yourcustomvisionapikey* -n CucumberOrCourgette -p unseen/cucumber.jpg -q
```





