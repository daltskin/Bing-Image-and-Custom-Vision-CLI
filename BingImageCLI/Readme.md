# Bing Image CLI - Bing Image Search API Results - Image Downloader
Download images from Bing to your local machine using Bing Search APIs - [Bing Image Search](https://docs.microsoft.com/en-us/azure/cognitive-services/Bing-Image-Search/overview).  For documentation on the underlying API see: https://docs.microsoft.com/en-us/rest/api/cognitiveservices-bingsearch/bing-images-api-v7-reference

## Bing Image API Key
To retrieve your Bing Image API key start here: https://docs.microsoft.com/en-us/azure/cognitive-services/Bing-Image-Search/overview#workflow

## Arguments

| Argument name | shortcut | default | values |
|----|----|----|----|
| BingUri | -u | https://api.cognitive.microsoft.com/bing/v7.0/images/search | |
| BingAPIKey | -k | | |
| SearchTerm | -q | | |
| DestintationPath | -p | | |
| MaxResultCount | -m | 35 | 1-150 |
| Size | -s| All | Small, Medium, Large, Wallpaper, All |
| Safe Search filter| -ss | Strict | Off, Moderate, Strict |
|Licence | -l| | Any, Public, Share, ShareCommercially, Modify, ModifyCommercially, All |

## Usage

### Simple query
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -p c:\photos
```

### Multiple search terms query
```
BingImageCLI.exe -k *yourbingapikey* -q "Microsoft Clippy, paperclip, office" -p c:\photos
```

### Return first 50 results only
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -p c:\photos -m 50
```

### Return public licensed images only
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -p c:\photos -l Public
```

### Remove safe filter from results
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -p c:\photos -ss Off
```

### Return large images (> 500x500)
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -p c:\photos -s Large
```



