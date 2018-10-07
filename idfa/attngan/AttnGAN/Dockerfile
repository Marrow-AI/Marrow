FROM continuumio/miniconda

MAINTAINER cv965@nyu.edu

RUN apt-get update
RUN apt-get install nano
RUN apt-get install wget

RUN conda install pytorch torchvision -c pytorch
RUN pip install python-dateutil easydict pandas torchfile nltk scikit-image
RUN pip install gdown

RUN git clone https://github.com/taoxugit/AttnGAN.git
WORKDIR /AttnGAN/data

# birds metadata
RUN gdown https://drive.google.com/uc?id=1O_LtUP9sch09QH3s_EBAgLEctBQ5JBSJ
# coco metadata
RUN gdown https://drive.google.com/uc?id=1rSnbIGNDGZeHlsUlLdahj0RJ9oo6lgH9

# pretrained models
WORKDIR /AttnGAN/DAMSMencoders
RUN gdown https://drive.google.com/uc?id=1GNUKjVeyWYBJ8hEU-yrfYQpDOkxEyP3V
RUN gdown https://drive.google.com/open?id=1zIrXCE9F6yfbEJIbNP5-YrEe2pZcPSGJ
RUN gdown https://drive.google.com/open?id=1lqNG75suOuR_8gjoEPYNp8VyT_ufPPig
RUN gdown https://drive.google.com/open?id=1i9Xkg9nU74RAvkcqKE-rJYhjvzKAMnCi
RUN gdown https://drive.google.com/open?id=19TG0JUoXurxsmZLaJ82Yo6O0UJ6aDBpg

WORKDIR /AttnGAN