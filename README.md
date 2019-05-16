# Marrow - GAN

## Marraw's#1 architecture
![first](https://i.imgur.com/dqSux5C.jpg)

## Starting the project

### Running the EC2 instance and dockers

Make sure you have the private key file in the right location and permissions. Add the key to your ssh-agent
```
cd ~/.ssh
cp ~/Downloads/marrow.pem .
chmod 400 marrow.pem
ssh-add marrow.pem
```
Go to the login link: https://953902915045.signin.aws.amazon.com/console and to Services->EC2, launch the instance.

#### important links / information
- Python version 3.6 (works best with the speech engine). 
- {url}/ images datasets url: https://drive.google.com/drive/folders/1uzHGGHLnTHXtTdR_HgwxevnYxr8Y69I0?usp=sharing (for the image slider that starts the experience). 
- pix2pix and densepose servers need to run together. 
- for this version we ended up using Google speech to text cloud services. 



