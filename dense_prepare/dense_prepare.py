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

  t2 = time.time()

  logger.info('Inference time: {:.3f}s'.format(t2 - t))
  process_bodies(img, cls_boxes, cls_bodys)

  #cv2.imwrite(os.path.join(output_dir, '{}'.format('opencv.png')), r )

  #file_content=base64.b64decode(r)
  #with open("stylegan/pose.jpg","wb") as f:
	#f.write(file_content)


def process_bodies(im, boxes, body_uv):
		if isinstance(boxes, list):
				box_list = [b for b in boxes if len(b) > 0]
				if len(box_list) > 0:
						boxes = np.concatenate(box_list)
				else:
						boxes = None

		IUV_fields = body_uv[1]
		All_Coords = np.zeros(im.shape)
		print("Shape of image {}".format(im.shape))
		All_inds = np.zeros([im.shape[0],im.shape[1]])
		K = 26

		inds = np.argsort(boxes[:,4])
		good_inds = inds[boxes[inds[:],4] >= 0.85]
		print("Indexes {}".format(inds))
		print('Found {} people'.format(len(good_inds)))

		Final = np.zeros([512,512, 3])
		current_offset = 0

		np.random.shuffle(good_inds)
		print("Good index {}".format(good_inds))

		if len(good_inds) >= 4:
				for i, ind in enumerate(good_inds):
						entry = boxes[ind,:]
						print(entry[4])
						entry=entry[0:4].astype(int)
						####
						output = IUV_fields[ind]
						####
						###
						CurrentMask = (output[0,:,:]>0).astype(np.float32)
						BlackMask = (output[0,:,:]==0)
						print("Shape of mask {}".format(CurrentMask.shape))
						print("Shape of output {}".format(output.shape))
						#All_Coords = np.zeros([output.shape[1],output.shape[2], 3])
						#All_Coords_Old = All_Coords[:output.shape[1],:output.shape[2],:]
						#All_Coords_Old[All_Coords_Old==0]= im[entry[1]:output.shape[1]+entry[1],entry[0]:output.shape[2]+entry[0],:][All_Coords_Old==0]
						#All_Coords[ 0 : output.shape[1], 0 : output.shape[2],:]= All_Coords_Old

						All_Coords = np.array(im[entry[1]:output.shape[1]+entry[1],entry[0]:output.shape[2]+entry[0],:])
						cv2.imwrite(os.path.join(output_dir, 'coords{}.png'.format(ind)), All_Coords)
						cv2.imwrite(os.path.join(output_dir, 'img{}.png'.format(ind)), im)
						All_Coords[BlackMask] = 0
						All_Coords = All_Coords.astype(np.uint8)
						resize_ratio = 128 / output.shape[2]
						print("Resize ratio {} ({})".format(resize_ratio, output.shape[2]))
						img = cv2.resize(All_Coords, (128,int(output.shape[1] * resize_ratio)))
						cv2.imwrite(os.path.join(output_dir, 'person{}.png'.format(ind)), img)

						Final[128:img.shape[0]+128,current_offset:img.shape[1] + current_offset,:] = img

						current_offset = current_offset + 128


						#All_inds = np.zeros([CurrentMask.shape[0],CurrentMask.shape[1]])
						#All_inds_old = All_inds[ entry[1] : entry[1]+output.shape[1],entry[0]:entry[0]+output.shape[2]]
						#All_inds_old = All_inds[0 : output.shape[1],0: output.shape[2]]
						#All_inds_old[All_inds_old==0] = CurrentMask[All_inds_old==0]*i
						#All_inds_old[All_inds_old==0] = CurrentMask[All_inds_old==0]*255
						#All_inds_old[All_inds_old==0] = CurrentMask[All_inds_old==0]*255
						#All_inds[ entry[1] : entry[1]+output.shape[1],entry[0]:entry[0]+output.shape[2]] = All_inds_old
						#All_inds[ 0: output.shape[1],0 : output.shape[2]] = All_inds_old

				#All_Coords[:,:,1:3] = 255. * All_Coords[:,:,1:3]
				#All_Coords[All_Coords>255] = 255.
				#All_inds = All_inds.astype(np.uint8)
				#cv2.imwrite(os.path.join(output_dir, '{}'.format('inds3.png')), All_inds )
				#cv2.imwrite(os.path.join(output_dir, '{}'.format('coords4.png')), All_Coords)
				#cv2.imwrite(os.path.join(output_dir, '{}'.format('img.png')), img)
				cv2.imwrite(os.path.join(output_dir, '{}'.format('final.png')), Final)

def vis_one_image(
        im, im_name, output_dir, boxes, segms=None, keypoints=None, body_uv=None, thresh=0.9,
        kp_thresh=2, dpi=200, box_alpha=0.0, dataset=None, show_class=False,
        ext='pdf'):
    """Visual debugging of detections."""
    #if not os.path.exists(output_dir):
    #    os.makedirs(output_dir)

    t0 = time.time() 

    if isinstance(boxes, list):
        boxes, segms, keypoints, classes = convert_from_cls_format(
            boxes, segms, keypoints)

    print(time.time() - t0)

    if boxes is None or boxes.shape[0] == 0 or max(boxes[:, 4]) < thresh:
        return
    IUV_fields = body_uv[1]
    #
    All_Coords = np.zeros(im.shape)
    All_inds = np.zeros([im.shape[0],im.shape[1]])
    K = 26
    ##
    inds = np.argsort(boxes[:,4])
    ##
    t1 = time.time()
    for i, ind in enumerate(inds):
        entry = boxes[ind,:]
        if entry[4] > 0.85:
	    print('ENTRY {}'.format(entry[4]))
            entry=entry[0:4].astype(int)
            ####
            output = IUV_fields[ind]
            ####
            All_Coords_Old = All_Coords[ entry[1] : entry[1]+output.shape[1],entry[0]:entry[0]+output.shape[2],:]
            All_Coords_Old[All_Coords_Old==0]=output.transpose([1,2,0])[All_Coords_Old==0]
            All_Coords[ entry[1] : entry[1]+output.shape[1],entry[0]:entry[0]+output.shape[2],:]= All_Coords_Old
            ###
            CurrentMask = (output[0,:,:]>0).astype(np.float32)
            All_inds_old = All_inds[ entry[1] : entry[1]+output.shape[1],entry[0]:entry[0]+output.shape[2]]
            #All_inds_old[All_inds_old==0] = CurrentMask[All_inds_old==0]*i
            All_inds_old[All_inds_old==0] = CurrentMask[All_inds_old==0]*255
            All_inds[ entry[1] : entry[1]+output.shape[1],entry[0]:entry[0]+output.shape[2]] = All_inds_old

    print(time.time() - t1)
    All_Coords[:,:,1:3] = 255. * All_Coords[:,:,1:3]
    All_Coords[All_Coords>255] = 255.
    All_Coords = All_Coords.astype(np.uint8)
    All_inds = All_inds.astype(np.uint8)
    cv2.imwrite(os.path.join(output_dir, '{}'.format('inds.png')), All_inds )
    print('IUV written to: ' , os.path.join(output_dir, '{}'.format('inds.png')) )
    img = All_Coords
    img = imresize(img,  (512, 1080), interp='bilinear')
    retval, buffer = cv2.imencode('.jpg', img)
    cv2.imwrite(os.path.join(output_dir, '{}'.format('coords.jpg')), img )
    t = time.time()
    jpg_as_text = base64.b64encode(buffer)
    print('Encoding time: %f' % (time.time() - t))
    return jpg_as_text

  


main()
