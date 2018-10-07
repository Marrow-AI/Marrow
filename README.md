# Marrow


# Generative Models

## AttnGAN

The `/AttnGAN` folder contains the Pytorch code for the AttnGAN implementation that reproduces the paper [AttnGAN: Fine-Grained Text to Image Generation with Attentional Generative Adversarial Networks](https://arxiv.org/pdf/1711.10485.pdf)

### Building

From custom Dockerfile:

```
docker build -t attgan:latest -f Dockerfile .
```