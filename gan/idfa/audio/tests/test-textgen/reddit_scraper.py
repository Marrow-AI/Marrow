import requests
import csv
from bs4 import BeautifulSoup

if __name__ == "__main__":
    url = "https://old.reddit.com/r/confessions/"
    headers = {'User-Agent': 'Mozilla/5.0'}
    page = requests.get(url, headers=headers)
    soup = BeautifulSoup(page.text, 'html.parser')

    attrs = {'class': 'thing', 'data-subreddit': 'confessions'}
    counter = 1
    print("Parsed")
    with open('confessions.txt', 'a') as output:
        while counter <= 5000:
            for post in soup.find_all("div", attrs=attrs):
                title = post.find("a", class_="title").text
                word_count = len(title.split())
                if word_count > 1:
                    print(title)
                    output.write(title + '\n' )
                    counter += 1
            
            next_button = soup.find("span", class_="next-button")
            next_page_link = next_button.find("a").attrs['href']
            page = requests.get(next_page_link, headers=headers)
            soup = BeautifulSoup(page.text, 'html.parser')
