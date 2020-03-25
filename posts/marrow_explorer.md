# Developers and Artists: Exploring the latent space of GAN hand in hand

> In January 2020 we finalized the development phase of _Marrow_. In this series of posts, Shirin Anlen and myself are sharing what we have learned during this process. The [first post](https://medium.com/@s.h.i.r.i.n/7f7db708f06d) was about constructing a well-formed dataset, despite having little resources. This post is about working with artists when exploring the latent space of GAN

![Shadow animation](./Shadow_animation1.gif)

## Myself and Marrow

Marrow is a hands-on research initiative and interactive installation by [Shirin Anlen](https://shirin.works), exploring the possibilities of [mental disorders in machine learning](https://immerse.news/when-machines-look-for-order-in-chaos-198fb222b60a). I have previously worked with Shirin on numerous projects, most notably the VR documentary [_Tzina: Symphony of Longing_](https://tzina.space). In 2018 I joined forces with Shirin once more to preview _Marrow_ as an installation at [IDFA Doclab 2018](https://www.doclab.org/2018/ive-always-been-jealous-of-other-peoples-families/). The prototype was a success, and one year later we went as co-creators to an intensive development phase produced by the [National Film Board of Canada](https://www.nfb.ca/interactive/marrow/) and [Atlas V](https://atlasv.io/).

## Animating over the latent space

The [previous post](https://medium.com/@s.h.i.r.i.n/7f7db708f06d) described how we created a dataset of _Perfect family dinners_ and used it to train [StyleGAN V1](https://github.com/NVlabs/stylegan). The result wasn't perfect, but that was what we aimed for: A distorted image of what it means to be a happy family, if your entire life experience comes from internet stock images. 

Our dataset was a bundle of around 6,500 images containing aligned figures of four family members, stripped away from their family dinner setting. Once StyleGAN finished training (in fact we stopped it once the quality started to deteriorate), we ended up with a vast space of possible new images containing four distorted familial figures. The infinite, continuous, space of possibilities for output images, that is controlled by seemingly arbitrary numerical variables is called the [_Latent Space_](https://en.wikipedia.org/wiki/Latent_variable)






