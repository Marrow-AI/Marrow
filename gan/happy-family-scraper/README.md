# Happy Family Scraper

## Requirements

-   `Python 3+` (tested with `3.6.8 64-bit` & `3.7.0 64-bit)
-   `Chromium` or `Google Chrome` installed on the system (Mac, PC or Linux doesn't matter)

## Installation

1. Install `python 3+` and following dependencies
2. Install Chrome on your OS
3. (Optionally) Set path to Chrome binary in `Makefile`

## Python Dependencies

```bash
pip install -U requests
pip install -U selenium
pip install -U Pillow
pip install -U google_images_download
pip install -U beautifulsoup4
pip install -U lxml
```

## Run Scapers

```bash
# Google Image Search
SEARCH_TERM="happy family dinner" PAGECOUNT=200 START_PAGE=1 make google
```

```bash
# BigStock Image Search
SEARCH_TERM="happy family dinner" PAGECOUNT=200 START_PAGE=1 make bigstock
```

```bash
# ShutterStock Image Search
SEARCH_TERM="happy family dinner" PAGECOUNT=200 START_PAGE=1 make shutterstock
```

## Removing bottom watermark on `BigStock` images

After running the scraper for `BigStock`:

```bash
IMAGE_INPUT_PATH="datasets/shutterstock/happy-family-dinner" make bigstock-unmark
```

## Removing bottom watermark on `ShutterStock` images

After running the scraper for `ShutterStock`:

```bash
IMAGE_INPUT_PATH="datasets/shutterstock/family-of-four" make shutterstock-unmark
```

## Converting images to JPEG with sRGB color profile

After running the scraper:

```bash
IMAGE_INPUT_PATH="datasets/shutterstock/family-of-four" make convert-to-jpeg
```

## Datasets

https://drive.google.com/drive/u/0/folders/1LjxoHqG9PmP2WHA61BF0mWLfWtN7-KF4
