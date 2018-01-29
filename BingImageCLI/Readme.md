# Bing Image CLI - Bing Image Search API Results - Image Downloader
Download images from Bing to your local machine using Microsoft Cognitive Services - Bing Image Search.  To learn more about Bing Image Search API, please see here: https://azure.microsoft.com/en-gb/services/cognitive-services/bing-image-search-api/

## Bing Image API Key
To retrieve your Bing Image API key start here: https://azure.microsoft.com/en-gb/try/cognitive-services/?api=bing-image-search-api

## Arguments

| Argument name | shortcut | default | values |
|----|----|----|----|
| BingUri | -u | https://api.cognitive.microsoft.com/bing/v7.0 | |
| BingAPIKey | -k | | |
| SearchTerm | -s | | |
| DestintationPath | -p | | |
| MaxResultCount | -m | 35 | 1-150 |
| MaxFileSize | -fmax | | bytes |
| MinFileSize | -fmin | | bytes |
| Safe Search filter| -ss | Strict | Off, Moderate, Strict |
|Licence | -l| | Any, Public, Share, ShareCommercially, Modify, ModifyCommercially, All |

## Usage

### Simple query
```
BingImageCLI.exe -k *yourbingapikey* -s Clippy -p c:\photos
```

### Multiple search terms query
```
BingImageCLI.exe -k *yourbingapikey* -s "Microsoft Clippy, paperclip, office" -p c:\photos
```

### Return first 50 results only
```
BingImageCLI.exe -k *yourbingapikey* -s Clippy -p c:\photos -m 50
```

### Return public licensed images only
```
BingImageCLI.exe -k *yourbingapikey* -s Clippy -p c:\photos -l Public
```

### Remove safe filter from results
```
BingImageCLI.exe -k *yourbingapikey* -s Clippy -p c:\photos -ss Off
```

### Return images of between 1-4MB
```
BingImageCLI.exe -k *yourbingapikey* -s Clippy -p c:\photos -fmin 1000000 -fmax 4000000
```



