from urllib.parse import urlparse
from selenium import webdriver
from bs4 import BeautifulSoup

import os
import re
import requests
import shutil
import ssl

ssl._create_default_https_context = ssl._create_unverified_context


class ShutterStockScraper:
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/537.36",
        "Accept-Encoding": "identity",
    }

    def __init__(self, happy: "Happy"):
        self.happy = happy

    def run(self, search_options, driver_options):
        searchterm = search_options["searchterm"]
        pagecount = search_options["pagecount"]
        start_page = search_options["start_page"]
        image_size = search_options["image_size"]

        # Setup Browser
        options = webdriver.ChromeOptions()
        options.binary_location = driver_options["chrome_binary_path"]
        options.add_argument("--no-sandbox")

        self.browser = webdriver.Chrome(
            driver_options["chrome_driver_path"], options=options
        )
        self.browser.maximize_window()

        # Convert to kebab-case = "happy family dinner" -> "happy-family-dinner"
        searchterm_raw = searchterm
        searchterm = re.sub(r"\s+", "-", searchterm_raw)

        # image_type = "all" # all
        image_type = "photo"  # photo

        # Folder name
        folder_name = (
            f"datasets/shutterstock/{self.happy.cleanFilename(searchterm)}/raw"
        )

        success_counter = 0

        # Create a folder to store the images into
        if not os.path.exists(folder_name):
            os.makedirs(folder_name, exist_ok=True)

        # Pagination
        page_counter = start_page

        # Fetch all images
        try:
            for i in range(pagecount):
                url = f"https://www.shutterstock.com/search?searchterm={searchterm}&sort=popular&image_type={image_type}&search_source=base_landing_page&language=en&page={page_counter}"
                self.browser.get(url)

                data = self.browser.execute_script(
                    "return document.documentElement.outerHTML"
                )

                print("Page " + str(page_counter))

                scraper = BeautifulSoup(data, "lxml")
                img_container = scraper.find_all("img", {"class": "z_g_h"})

                for j in range(0, len(img_container) - 1):
                    img_src = img_container[j].get("src")

                    # Small (without watermark)
                    if image_size == "small":
                        img_url = img_src
                        img_name = img_src.rsplit("/", 1)[-1]

                    # Regular (with watermark)
                    elif image_size == "regular":
                        img_url = re.sub("260nw", "600w", img_src)
                        img_name = img_url.rsplit("/", 1)[-1]

                    # Large (with different watermark)
                    else:
                        # Keep original file name
                        img_name = img_src.rsplit("/", 1)[-1]
                        img_id = img_name.rsplit("-")[-1]
                        img_ext = os.path.splitext(urlparse(img_src).path)[1]
                        img_ext_dotless = re.sub("\.", "", img_ext)

                        # Large image url
                        img_url = f"https://image.shutterstock.com/z/photo-slug-{img_id}.{img_ext_dotless}"

                    try:
                        # Fetch image
                        file_dest = os.path.join(folder_name, img_name)

                        with requests.get(
                            img_url, headers=self.headers, stream=True
                        ) as response:
                            with open(file_dest, "wb") as out_file:

                                if response.ok:
                                    response.raw.decode_content = True
                                    shutil.copyfileobj(response.raw, out_file)

                                    success_counter = success_counter + 1

                                    print("Scraped " + img_name)
                                else:
                                    print("Error: " + response.text)

                    except Exception as e:
                        print(e)

                page_counter += 1

        except Exception as e:
            print("Error: could not fetch page.")
            print(e)
            self.browser.close()

        print(f"{success_counter} pictures successfully downloaded.")
        self.browser.close()
