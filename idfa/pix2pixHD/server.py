# pix2pix server
# Run inference over a single image
# =================
import time
import os
import json
import base64
import common
import io
import numpy as np
import gevent.monkey
gevent.monkey.patch_all()
from PIL import Image
from flask import Flask, jsonify, request
from flask_cors import CORS
from flask_socketio import SocketIO, emit
import cv2
from options.test_options import TestOptions
from models.models import create_model
import util.util as util
import torch
from data.base_dataset import get_params, get_transform
from scipy.misc import imresize
import torch.nn.functional as F

opt = None

def load_model(name):
  global opt
  opt = TestOptions().parse(save=False)
  opt.nThreads = 1
  opt.batchSize = 1
  opt.serial_batches = True
  opt.no_flip = True
  opt.name = name
  opt.resize_or_crop = 'none'
  opt.use_features = False
  opt.no_instance = True
  opt.label_nc = 0
  return create_model(opt)

current_model_name = 'dotherightthing'
current_model = load_model(current_model_name)

# Server configs
PORT = 23100
app = Flask(__name__)
CORS(app)
socketio = SocketIO(app)

# Take in base64 string and return PIL image
def stringToImage(base64_string):
  imgdata = base64.b64decode(base64_string)
  return Image.open(io.BytesIO(imgdata))

# Convert PIL Image to an RGB image(technically a numpy array) that's compatible with opencv
def toRGB(image):
  return cv2.cvtColor(np.array(image), cv2.COLOR_RGB2BGR)

def main(input_img):
  t1 = time.time()
  raw_img = stringToImage(input_img)
  params = get_params(opt, raw_img.size)
  transform_label = get_transform(opt, params, method=Image.NEAREST, normalize=False)
  label_tensor = transform_label(raw_img)
  label_tensor = label_tensor.unsqueeze(0)
  # Get fake image
  generated = current_model.inference(label_tensor, None)
  torch.cuda.synchronize()
  # Save img
  print(time.time() - t1)
  im = util.tensor2im(generated.data[0])
  im_pil = Image.fromarray(im)
  buffer = io.BytesIO()
  im_pil.save(buffer, format='JPEG')

  return base64.b64encode(buffer.getvalue())

# --- 
# Server Routes
# --- 
@app.route('/')
def index():
  return jsonify(status="200", message='pix2pixHD is running')

@app.route('/infer', methods=['POST'])
def infer():
  results = main(request.form['data'])
  return results

if __name__ == '__main__':
  socketio.run(app, host='0.0.0.0', port=PORT, debug=False)