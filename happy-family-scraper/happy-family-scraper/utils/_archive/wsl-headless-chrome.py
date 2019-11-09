#!/usr/bin/python3

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from pyvirtualdisplay import Display

display = Display(visible=0, size=(1280, 1024))
display.start()

options = Options()
options.binary_location = '/usr/bin/google-chrome'
options.add_argument('--window-size=1280,1024')
options.add_argument('--no-sandbox')
options.add_argument('--disable-dev-shm-usage')
prefs = {
	"download.prompt_for_download": False,
	"download.default_directory": "/path/to/download/dir",
	"download.directory_upgrade": True,
	"profile.default_content_settings.popups": 0,
	"plugins.plugins_disabled":["Chrome PDF Viewer"],
	"plugins.always_open_pdf_externally": True,
}
options.add_experimental_option("prefs",prefs)
driver = webdriver.Chrome('/usr/local/bin/chromedriver', chrome_options=options)

driver.get('https://www.google.com')

driver.save_screenshot("screenshot.png")

driver.quit()
display.stop()