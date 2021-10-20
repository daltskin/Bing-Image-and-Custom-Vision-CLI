# Bing Image Downloader CLI
Download images from Bing to your local machine using Bing Search APIs - [Bing Image Search](https://docs.microsoft.com/en-us/azure/cognitive-services/Bing-Image-Search/overview).  For documentation on the underlying API see: https://docs.microsoft.com/en-us/rest/api/cognitiveservices-bingsearch/bing-images-api-v7-reference

## Bing Image API Key
To retrieve your Bing Image API key start here: https://docs.microsoft.com/en-us/azure/cognitive-services/Bing-Image-Search/overview#workflow

## Arguments

| Argument name | shortcut | default | values |
|----|----|----|----|
| BingUri | -u | https://api.cognitive.microsoft.com/bing/v7.0/images/search | |
| BingAPIKey | -k | | |
| SearchTerm | -q | | |
| MaxResultCount | -m | 35 | 1-150 |
| Size | -s | All | Small, Medium, Large, Wallpaper, All |
| Safe Search filter| -ss | Strict | Off, Moderate, Strict |
| Licence | -l| All | Any, Public, Share, ShareCommercially, Modify, ModifyCommercially, All |

## Usage

### Simple query
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy
```

### Multiple search terms query
```
BingImageCLI.exe -k *yourbingapikey* -q "Microsoft Clippy, paperclip, office"
```

### Return first 50 results only
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -m 50
```

### Return public licensed images only
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -l Public
```

### Remove safe filter from results
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -ss Off
```

### Return large images (> 500x500)
```
BingImageCLI.exe -k *yourbingapikey* -q Clippy -s Large
```



