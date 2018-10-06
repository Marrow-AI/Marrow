# Marrow


# Generative Models

## AttnGAN

The `/AttnGAN` folder contains the Pytorch code for the AttnGAN implementation that reproduces the paper [AttnGAN: Fine-Grained Text to Image Generation with Attentional Generative Adversarial Networks](https://arxiv.org/pdf/1711.10485.pdf)

### Building

From custom Dockerfile:

```
docker build -t attgan:latest -f Dockerfile .
```


## Download pretrained data and models

Preprocessed metadata for birds. save them to data/
```sh
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1O_LtUP9sch09QH3s_EBAgLEctBQ5JBSJ' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1O_LtUP9sch09QH3s_EBAgLEctBQ5JBSJ" -O birds.zip && rm -rf /tmp/cookies.txt
```

Preprocessed metadata for coco. save them to data/
```sh
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1rSnbIGNDGZeHlsUlLdahj0RJ9oo6lgH9' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1rSnbIGNDGZeHlsUlLdahj0RJ9oo6lgH9" -O coco.zip && rm -rf /tmp/cookies.txt
```

Birds Image Data: Extract them to data/birds/
```sh
wget http://www.vision.caltech.edu/visipedia-data/CUB-200-2011/CUB_200_2011.tgz
```

COCO Data: Extract them to data/coco/
```sh

```

## Models

DAMSM for bird
```sh
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1GNUKjVeyWYBJ8hEU-yrfYQpDOkxEyP3V' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1GNUKjVeyWYBJ8hEU-yrfYQpDOkxEyP3V" -O bird.zip && rm -rf /tmp/cookies.txt
```

DAMSM for coco
```sh
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1zIrXCE9F6yfbEJIbNP5-YrEe2pZcPSGJ' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1zIrXCE9F6yfbEJIbNP5-YrEe2pZcPSGJ" -O coco.zip && rm -rf /tmp/cookies.txt
```

AttnGAN for bird. 
```sh
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1lqNG75suOuR_8gjoEPYNp8VyT_ufPPig' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1lqNG75suOuR_8gjoEPYNp8VyT_ufPPig" -O bird_AttnGAN2.pth && rm -rf /tmp/cookies.txt
```

AttnGAN for coco. 
```sh
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1i9Xkg9nU74RAvkcqKE-rJYhjvzKAMnCi' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1i9Xkg9nU74RAvkcqKE-rJYhjvzKAMnCi" -O coco_AttnGAN2.pth && rm -rf /tmp/cookies.txt
```

AttnDCGAN for bird
```sh
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=19TG0JUoXurxsmZLaJ82Yo6O0UJ6aDBpg' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=19TG0JUoXurxsmZLaJ82Yo6O0UJ6aDBpg" -O bird_AttnDCGAN2.pth && rm -rf /tmp/cookies.txt
```
