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

sys.path.append('./server')
from vis_utils import vis_one_image
from vis_utils import vis_one_image_opencv

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

# Main
def main():
  #image = stringToImage(input_img[input_img.find(",")+1:])
  img = cv2.imread('stylegan/narrow/13.test.jpg')
  #img = toRGB(image)
  logger.info('Processing {} -> {}'.format('New Image', 'Output...'))
  timers = defaultdict(Timer)
  t = time.time()
  size = img.shape[:2]
  with c2_utils.NamedCudaScope(0):
    cls_boxes, cls_segms, cls_keyps, cls_bodys = infer_engine.im_detect_all(
      model, img, None, timers=timers
    )
  for key, timer in timers.items():
    print(key, timer.total_time)
  t2 = time.time()
  print('SEGMS {}'.format(cls_segms))
  r = vis_one_image(
  #r = vis_one_image_opencv(
		img, 
	'testImage',
		output_dir,
		cls_boxes,
		segms=cls_segms,
		keypoints=cls_keyps,
		body_uv=cls_bodys,
		dataset=dummy_coco_dataset, 
		#box_alpha=0.3, 
		show_class=True, 
	  #show_box=False,
		thresh=0.7, 
		kp_thresh=2
	)
  t3 = time.time()

  logger.info('Inference time: {:.3f}s'.format(t2 - t))
  logger.info('Visualization time: {:.3f}s'.format(t3 - t2))

  #cv2.imwrite(os.path.join(output_dir, '{}'.format('opencv.png')), r )

  file_content=base64.b64decode(r)
  with open("stylegan/pose.jpg","wb") as f:
		f.write(file_content)


  


main()
