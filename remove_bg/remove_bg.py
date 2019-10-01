#!/usr/bin/python3
import argparse
import base64
import requests
import json
import cv2

parser = argparse.ArgumentParser(description='Remove background using DensePose')
parser.add_argument('input_image_file', metavar='Input image file', help='Image file name')
parser.add_argument('output_image_file', metavar='Output image file', help='Image file name')

args = parser.parse_args()

DENSEPOSE_URL = 'http://localhost:22100/pose'


with open(args.input_image_file, "rb") as image_file:
    image_string = base64.b64encode(image_file.read()).decode('ascii')
    print('Getting pose')
    r = requests.post(DENSEPOSE_URL, json = {'data': image_string})
    file_content=base64.b64decode(r.json()['results'])
    with open("/tmp/pose.jpg","wb") as f:
        f.write(file_content)

    print('Masking')
    src1 = cv2.imread(args.input_image_file)
    src2 = cv2.imread('/tmp/pose.jpg')
    gray = cv2.cvtColor(src2, cv2.COLOR_BGR2GRAY)
    ret,black_white = cv2.threshold(gray,1,255,cv2.THRESH_BINARY)
    #cv2.imwrite('black_white.jpg', black_white)
    mask = cv2.cvtColor(black_white, cv2.COLOR_GRAY2RGB)
    mask = cv2.resize(mask, src1.shape[1::-1])
    dst = cv2.bitwise_and(src1, mask)
    print('Writing')
    cv2.imwrite(args.output_image_file, dst)



