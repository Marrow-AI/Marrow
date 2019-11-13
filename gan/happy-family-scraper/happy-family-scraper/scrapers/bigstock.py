from urllib.parse import urlparse
from selenium import webdriver
from selenium.webdriver.common.keys import Keys

import os
import re
import requests
import shutil


class BigStockScraper:
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
        self.browser = webdriver.Chrome(
            driver_options["chrome_driver_path"], options=options
        )
        self.browser.maximize_window()

        # Convert to kebab-case = "happy family dinner" -> "happy-family-dinner"
        searchterm_raw = search_options["searchterm"]
        searchterm = re.sub(r"\s+", "-", searchterm_raw)

        # Folder name
        folder_name = f"datasets/bigstock/{self.happy.cleanFilename(searchterm)}/raw"

        counter = 0
        success_counter = 0

        # Create a folder to store the images into
        if not os.path.exists(folder_name):
            os.makedirs(folder_name, exist_ok=True)

        # Pagination
        page_counter = start_page

        # Fetch all images within pagecount
        for _ in range(pagecount):
            try:
                url = f"https://www.bigstockphoto.com/search/{searchterm}?start={page_counter}"
                self.browser.get(url)

                # Count images in current page
                img_count = self.browser.execute_script(
                    "return Array.from(document.querySelectorAll('.mosaic_cell')).length"
                )

                print("Page " + str(page_counter))

                # Next page will start from last fetched image
                start_page += img_count

                for x in self.browser.find_elements_by_xpath(
                    '//div[contains(@class,"media")]//img'
                ):
                    counter = counter + 1

                    # URL
                    img_url = x.get_attribute("src")

                    # TODO: Implement image size cli argument here?
                    # Get URL for larger version of the image
                    img_url_large = re.sub("large2", "large1500", img_url)

                    # Image Name
                    img_name = img_url_large.rsplit("/", 1)[-1]

                    # Extension
                    # imgtype = os.path.splitext(urlparse(img_url_large).path)[1]

                    # Remove dot in extension
                    # imgtype_no_dot = re.sub("\.", "", imgtype)

                    # print(
                    #     f"Total Count: {counter}. Successful Count: {success_counter}. URL: {img_url_large}."
                    # )

                    try:
                        # Fetch image
                        file_dest = os.path.join(folder_name, img_name)

                        with requests.get(
                            img_url_large, headers=self.headers, stream=True
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
                        print("Error: could not fetch image.")
                        print(e)

                page_counter += 1

            except Exception as e:
                print("Error: could not fetch page.")
                print(e)
                self.browser.close()

        print(f"{success_counter} pictures successfully downloaded.")
        self.browser.close()

    def goToNextPage(self, browser):
        browser.execute_script(
            "document.querySelector('.search-pagination-next').click()"
        )
