#!/usr/bin/python3
import argparse
import base64
import requests
import json
import cv2

parser = argparse.ArgumentParser(description='Remove background using DensePose')
parser.add_argument('image_file', metavar='Image file', help='Image file name')

args = parser.parse_args()

DENSEPOSE_URL = 'http://52.206.213.41:22100/pose'


with open(args.image_file, "rb") as image_file:

    src = cv2.imread(args.image_file)
    gray = cv2.cvtColor(src, cv2.COLOR_BGR2GRAY)

    edged = cv2.Canny(gray, 30, 200)
    cv2.waitKey(0)

    contours, hierarchy = cv2.findContours(edged,
        cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)

    cv2.imshow('Canny Edges After Contouring', edged)
    cv2.waitKey(0)
    cv2.destroyAllWindows()


