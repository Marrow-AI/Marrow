#!/bin/sh
#export CUDA_VISIBLE_DEVICES=1
python record_ricoh.py live -c cocostuff164k.yaml -m deeplab-pytorch/data/models/deeplabv2_resnet101_msc-cocostuff164k-100000.pth --exp_name coco_pretrained --dataset_mode coco --dataroot SPADE/datasets/coco_stuff/ --checkpoints_dir ./SPADE/checkpoints
