from __future__ import absolute_import
from __future__ import division
from __future__ import print_function
from __future__ import unicode_literals

from collections import defaultdict
import argparse
import cv2
import glob
import logging
import requests
import os
import sys
import time
import json
import base64
import common
import io
import numpy as np
import random

from PIL import Image
from scipy.misc import imresize

from caffe2.python import workspace

from detectron.core.config import assert_and_infer_cfg
from detectron.core.config import cfg
from detectron.core.config import merge_cfg_from_file
from detectron.utils.io import cache_url
from detectron.utils.logging import setup_logging
from detectron.utils.timer import Timer
import detectron.core.test_engine as infer_engine
import detectron.datasets.dummy_datasets as dummy_datasets
import detectron.utils.c2 as c2_utils

from itertools import permutations

c2_utils.import_detectron_ops()

# OpenCL may be enabled by default in OpenCV3; disable it because it's not
# thread safe and causes unwanted GPU memory allocations.
cv2.ocl.setUseOpenCL(False)

# Densepose args
cfg_file = 'configs/DensePose_ResNet50_FPN_s1x-e2e.yaml'
weights_file = 'weights/DensePose_ResNet50_FPN_s1x-e2e.pkl'
output_dir = 'stylegan/dense_out/'
image_ext = 'jpg'
im_or_folder = ''
workspace.GlobalInit(['caffe2', '--caffe2_log_level=0'])
setup_logging(__name__)
logger = logging.getLogger(__name__)
merge_cfg_from_file(cfg_file)
cfg.NUM_GPUS = 1
cfg.TEST.BBOX_AUG.ENABLED = False
cfg.MODEL.MASK_ON = False
cfg.MODEL.KEYPOINTS_ON = False

# weights = cache_url(args.weights, cfg.DOWNLOAD_CACHE)
assert_and_infer_cfg(cache_urls=False)
model = infer_engine.initialize_model_from_cfg(weights_file)
dummy_coco_dataset = dummy_datasets.get_coco_dataset()

# Take in base64 string and return PIL image
def stringToImage(base64_string):
  imgdata = base64.b64decode(base64_string)
  return Image.open(io.BytesIO(imgdata))

# Convert PIL Image to an RGB image(technically a numpy array) that's compatible with opencv
def toRGB(image):
  return cv2.cvtColor(np.array(image), cv2.COLOR_BGR2RGB)


def run():
    parser = argparse.ArgumentParser(description='Family dataset filteration')
    parser.add_argument('input', metavar='input folder', help='Input folder with images')

    args = parser.parse_args()

    counter = 0 
    file_counter = 1 
    print('Processing {}'.format(args.input))
    image_files = os.listdir(args.input)
    for image_file in image_files:
        try:
            file_no_ext = os.path.splitext(image_file)[0]
            (result, num_people) = process_image(args.input + image_file)
            if result:
                counter = counter + 1
                print("{} PASS ({}) ({}/{})".format(image_file, num_people,counter, file_counter))
            else:
                print("{} FAIL ({})".format(image_file, num_people))

        except Exception as e:
            print("{} FAIL ({})".format(image_file, 'ERR'))
            continue
        finally:
            file_counter = file_counter + 1



def process_image(input_file):
  img = cv2.imread(input_file)
  size = img.shape[:2]

  with c2_utils.NamedCudaScope(0):
    cls_boxes, cls_segms, cls_keyps, cls_bodys = infer_engine.im_detect_all(
      model, img, None, timers=timers
    )

  t2 = time.time()

  # Return true if there are 4 persons in the image
  if cls_bodys is not None:
    num_people = process_bodies(img, cls_boxes, cls_bodys)
    if num_people == 4:
      return (True, num_people)
    else:
      return (False, num_people)
  else:
    return (False, 0)


def process_bodies(im, boxes, body_uv):
    if isinstance(boxes, list):
        box_list = [b for b in boxes if len(b) > 0]
        if len(box_list) > 0:
            boxes = np.concatenate(box_list)
        else:
            boxes = None

    IUV_fields = body_uv[1]
    K = 26

    inds = np.argsort(boxes[:,0])
    good_inds = inds[boxes[inds[:],4] >= 0.95] # Certainty threshold

    num_people = len(good_inds)

    return num_people

run()
