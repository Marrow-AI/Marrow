# --- Variables ----------------------------------------------------------------------------------------


SEARCH_TERM ?= happy family dinner
PAGECOUNT ?= 100
START_PAGE ?= 1
IMAGE_INPUT_PATH ?= datasets/shutterstock/happy-family-dinner
IMAGE_SIZE ?= regular


# --- Detect OS ----------------------------------------------------------------------------------------


IS_WSL := $(shell cat /proc/sys/kernel/osrelease | grep 'Microsoft')
HAPPY_SCRAPER_OS ?= "linux"

ifneq ($(IS_WSL),)
	HAPPY_SCRAPER_OS = windows
	OSFLAG += -D WIN32
	ifeq ($(PROCESSOR_ARCHITECTURE),AMD64)
		OSFLAG += -D AMD64
	endif
	ifeq ($(PROCESSOR_ARCHITECTURE),x86)
		OSFLAG += -D IA32
	endif
else 
	OSFLAG 				:=
	ifeq ($(OS),Windows_NT)
		HAPPY_SCRAPER_OS = windows
		OSFLAG += -D WIN32
		ifeq ($(PROCESSOR_ARCHITECTURE),AMD64)
			OSFLAG += -D AMD64
		endif
		ifeq ($(PROCESSOR_ARCHITECTURE),x86)
			OSFLAG += -D IA32
		endif
	else
		UNAME_S := $(shell uname -s)
		ifeq ($(UNAME_S),Linux)
			HAPPY_SCRAPER_OS = linux
			OSFLAG += -D LINUX
		endif

		ifeq ($(UNAME_S),Darwin)
			HAPPY_SCRAPER_OS = osx
			OSFLAG += -D OSX
		endif

		UNAME_P := $(shell uname -p)
		ifeq ($(UNAME_P),x86_64)
			OSFLAG += -D AMD64
		endif

		ifneq ($(filter %86,$(UNAME_P)),)
			OSFLAG += -D IA32
		endif

		ifneq ($(filter arm%,$(UNAME_P)),)
			OSFLAG += -D ARM
		endif
	endif
endif

ifeq ($(HAPPY_SCRAPER_OS),windows)
	# Windows
	SCRAPER_CHROME_BINARY_PATH = C:\\Program Files (x86)\\Google\\Chrome\\Application\\Chrome.exe
	SCRAPER_CHROME_DRIVER_PATH = utils/chromedriver/chromedriver_win32/chromedriver.exe

	PYTHON_BIN = /usr/bin/python3
endif

ifeq ($(HAPPY_SCRAPER_OS),linux)
	# Linux
	SCRAPER_CHROME_BINARY_PATH = /usr/bin/chromium-browser
	SCRAPER_CHROME_DRIVER_PATH = utils/chromedriver/chromedriver_linux64/chromedriver

	PYTHON_BIN = /usr/bin/python3
endif

ifeq ($(HAPPY_SCRAPER_OS),osx)
	# Mac
	SCRAPER_CHROME_BINARY_PATH = /Applications/Google Chrome.app/Contents/MacOS/Google Chrome
	SCRAPER_CHROME_DRIVER_PATH = utils/chromedriver/chromedriver_mac64/chromedriver

	PYTHON_BIN = /Users/ozeki/.pyenv/shims/python
endif


# --- Commands ----------------------------------------------------------------------------------------


bigstock:
	HAPPY_SCRAPER_OS="$(HAPPY_SCRAPER_OS)" \
	SCRAPER_CHROME_BINARY_PATH="$(SCRAPER_CHROME_BINARY_PATH)" \
	SCRAPER_CHROME_DRIVER_PATH="$(SCRAPER_CHROME_DRIVER_PATH)" \
	IMAGE_SIZE="$(IMAGE_SIZE)" \
	$(PYTHON_BIN) happy.py bigstock "$(SEARCH_TERM)" $(PAGECOUNT) $(START_PAGE) "$(IMAGE_SIZE)"

google:
	HAPPY_SCRAPER_OS="$(HAPPY_SCRAPER_OS)" \
	SCRAPER_CHROME_BINARY_PATH="$(SCRAPER_CHROME_BINARY_PATH)" \
	SCRAPER_CHROME_DRIVER_PATH="$(SCRAPER_CHROME_DRIVER_PATH)" \
	IMAGE_SIZE="$(IMAGE_SIZE)" \
	$(PYTHON_BIN) happy.py google "$(SEARCH_TERM)" $(PAGECOUNT) $(START_PAGE) "$(IMAGE_SIZE)"

shutterstock:
	HAPPY_SCRAPER_OS="$(HAPPY_SCRAPER_OS)" \
	SCRAPER_CHROME_BINARY_PATH="$(SCRAPER_CHROME_BINARY_PATH)" \
	SCRAPER_CHROME_DRIVER_PATH="$(SCRAPER_CHROME_DRIVER_PATH)" \
	IMAGE_SIZE="$(IMAGE_SIZE)" \
	$(PYTHON_BIN) happy.py shutterstock "$(SEARCH_TERM)" $(PAGECOUNT) $(START_PAGE) "$(IMAGE_SIZE)"

bigstock-unmark:
	HAPPY_SCRAPER_OS="$(HAPPY_SCRAPER_OS)" \
	IMAGE_INPUT_PATH="$(IMAGE_INPUT_PATH)" \
	$(PYTHON_BIN) ./imaging/unmark-bigstock.py

shutterstock-unmark:
	HAPPY_SCRAPER_OS="$(HAPPY_SCRAPER_OS)" \
	IMAGE_INPUT_PATH="$(IMAGE_INPUT_PATH)" \
	$(PYTHON_BIN) ./imaging/unmark-shutterstock.py

convert-to-jpeg:
	HAPPY_SCRAPER_OS="$(HAPPY_SCRAPER_OS)" \
	IMAGE_INPUT_PATH="$(IMAGE_INPUT_PATH)" \
	$(PYTHON_BIN) ./imaging/convert-to-jpeg.py

env:
	@echo "OS: $(HAPPY_SCRAPER_OS)"
	@echo "OS Flags: $(OSFLAG)" 
	@echo "Image Size: $(IMAGE_SIZE)"
	@echo "Python Bin: $(PYTHON_BIN)" 
	@echo "Chrome Binary: $(SCRAPER_CHROME_BINARY_PATH)"
	@echo "Chrome Driver: $(SCRAPER_CHROME_DRIVER_PATH)"

# .PHONY: google bigstock shutterstock os convert-to-jpeg bigstock-unmark shutterstock-unmark