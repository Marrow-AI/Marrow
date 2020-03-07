# GAN : Happy families preparation scripts

- **dense_prepare.py**: Uses [DensePose](https://github.com/facebookresearch/DensePose) to extract four people out of an image and place them together in radnom orders.
- **dense_filter.py**:  Uses [DensePose](https://github.com/facebookresearch/DensePose) to filter out images that no longer have four discernible persons.
- **move_filtered.sh, remove_dupes.sh**: Helper scripts for discarding image files filtered by *dense_filter* and *[findimagedupes](https://gitlab.com/opennota/findimagedupes)* 
