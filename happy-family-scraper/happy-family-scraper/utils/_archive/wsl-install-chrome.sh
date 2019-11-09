# Taken from: https://blog.cles.jp/item/10821

# chrome repo
wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | sudo apt-key add -
sudo sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list'

# chrome
sudo apt update
# sudo apt install -y python3-pip python3-dev libgconf2-4 google-chrome-stable xvfb unzip
sudo apt install -y libgconf2-4 google-chrome-stable xvfb unzip
# sudo apt upgrade -y

# python
# sudo pip3 install pyvirtualdisplay transitions

# chromedriver
# wget https://chromedriver.storage.googleapis.com/73.0.3683.68/chromedriver_linux64.zip
# sudo unzip chromedriver_linux64.zip -d /usr/local/bin/
# sudo chmod 755 /usr/local/bin/chromedriver