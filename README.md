# Marrow - GAN

## Marraw's#1 architecture
![first](https://i.imgur.com/8VvJ57J.jpg)

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

