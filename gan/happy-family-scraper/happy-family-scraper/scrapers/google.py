from google_images_download import google_images_download

# from selenium import webdriver
# from selenium.webdriver.common.keys import Keys

import os

# import json
# import re
# import requests


class GoogleScraper:
    # headers = {
    #     "User-Agent": "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/537.36"
    # }

    def __init__(self, happy: "Happy"):
        self.happy = happy

    def run(self, search_options, driver_options):
        searchterm = search_options['searchterm']
        pagecount = search_options['pagecount']
        start_page = search_options["start_page"]
        image_size = search_options["image_size"]

        folder_name = f"datasets/google/{self.happy.cleanFilename(searchterm)}/raw"

        # Create a folder to store the images into
        if not os.path.exists(folder_name):
            os.makedirs(folder_name, exist_ok=True)

        # Google Images Download Library
        # Arguments:
        # https://google-images-download.readthedocs.io/en/latest/arguments.html
        response = google_images_download.googleimagesdownload()
        absolute_image_paths = response.download(
            {
                "keywords": searchterm,
                "type": "photo",
                
                # TODO: Implement image size argument here?
                "size": ">800*600",

                "limit": 10000,
                # "usage_rights": "labeled-for-reuse",
                # "delay": 0.3,
                "output_directory": folder_name,
                "print_urls": True,
                "chromedriver": driver_options["chrome_driver_path"],
            }
        )
