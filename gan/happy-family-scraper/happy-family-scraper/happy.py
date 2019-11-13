from scrapers.bigstock import BigStockScraper
from scrapers.google import GoogleScraper
from scrapers.shutterstock import ShutterStockScraper

import sys
import os
import unicodedata
import string
import argparse

valid_filename_chars = "-_.() %s%s" % (string.ascii_letters, string.digits)
char_limit = 255  # for safe windows filenames


class Happy:
    scraper = None
    browser = None
    site = ""
    searchterm = ""

    def execute(self, args):
        BINARY_PATH = os.getenv("SCRAPER_CHROME_BINARY_PATH")
        DRIVER_PATH = os.getenv("SCRAPER_CHROME_DRIVER_PATH")

        if BINARY_PATH is None or DRIVER_PATH is None:
            print(
                "Error: SCRAPER_CHROME_BINARY_PATH and SCRAPER_CHROME_DRIVER_PATH environment variables must be set to Selenium Chrome Driver settings."
            )
            return

        binary_path = BINARY_PATH
        driver_path = DRIVER_PATH

        # Parse arguments
        parser = argparse.ArgumentParser(
            description="Scrape a website for images using searchterms."
        )
        parser.add_argument(
            "site",
            type=str,
            help="The site to search in. Can be 'bigstock', 'google' or 'shutterstock'",
        )
        parser.add_argument("searchterm", type=str, help="The terms to search for.")
        parser.add_argument(
            "pagecount",
            type=int,
            default=100,
            help="The total number of pages to scrape. Default is 100.",
        )
        parser.add_argument(
            "start_page",
            type=int,
            default=1,
            help="The page to start the search on. Default is 1.",
        )
        parser.add_argument(
            "image_size",
            type=str,
            default="regular",
            help="The image size that should be downloaded. Can be 'small', 'regular' or 'large'. Default is 'regular'.",
        )

        arguments = parser.parse_args()

        self.site = arguments.site
        self.searchterm = arguments.searchterm
        self.pagecount = arguments.pagecount
        self.start_page = arguments.start_page
        self.image_size = arguments.image_size

        search_options = {
            "searchterm": self.searchterm,
            "pagecount": self.pagecount,
            "start_page": self.start_page,
            "image_size": self.image_size,
        }

        webdriver_options = {
            "chrome_binary_path": binary_path,
            "chrome_driver_path": driver_path,
        }

        # Select proper scraper based on received ards
        if self.site == "google":
            self.scraper = GoogleScraper(self)
            self.scraper.run(search_options, webdriver_options)

        elif self.site == "bigstock":
            self.scraper = BigStockScraper(self)
            self.scraper.run(search_options, webdriver_options)

        elif self.site == "shutterstock":
            self.scraper = ShutterStockScraper(self)
            self.scraper.run(search_options, webdriver_options)

        else:
            print(f"Error: no scraper found for website '{self.site}'.")

    def cleanFilename(self, filename, whitelist=valid_filename_chars, replace=" "):
        """
        Url: https://gist.github.com/wassname/1393c4a57cfcbf03641dbc31886123b8
        """
        # replace spaces
        for r in replace:
            filename = filename.replace(r, "_")

        # keep only valid ascii chars
        cleaned_filename = (
            unicodedata.normalize("NFKD", filename).encode("ASCII", "ignore").decode()
        )

        # keep only whitelisted chars
        cleaned_filename = "".join(c for c in cleaned_filename if c in whitelist)
        if len(cleaned_filename) > char_limit:
            print(
                "Warning, filename truncated because it was over {}. Filenames may no longer be unique".format(
                    char_limit
                )
            )
        return cleaned_filename[:char_limit]


if __name__ == "__main__":
    from sys import argv

    try:
        happy = Happy()
        happy.execute(argv)
    except KeyboardInterrupt:
        pass

    sys.exit()
