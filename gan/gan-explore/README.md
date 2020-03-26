# GAN : Latent Space Explorer 

- **marrow_explore.py**: Latent space explorer for [StyleGAN V1](https://github.com/NVlabs/stylegan). Copy file to StyleGAN's folder, or add it to the python library path. Run in a Tensorflow 1.10+ environment. Server will start at port 8080. Animation definitions are stored in the _animations_ subfolder. StyleGAN Snapshots folder is defined on this line:
```
    url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-{}.pkl".format(snapshot))
```
- **templates/index.html**:  Explorer frontend. Available snapshots are hard-coded at:
```
const SNAPSHOTS = [
    ..
]
```
