#!/usr/bin/env python
import pyaudio
import wave
import audioop
import webrtcvad
import argparse
import sys
import os
import numpy as np
from decoding import Decoder 
import wave
import sys
import struct
from datetime import datetime
import threading

from helper import *

#list up available input devices
def listup_devices():
    p = pyaudio.PyAudio()
    for i in range(p.get_device_count()):
        log(str(p.get_device_info_by_index(i)))

#find a device index by name
def find_device_id(name):
    p = pyaudio.PyAudio()
    info = p.get_host_api_info_by_index(0)
    numdevices = info.get('deviceCount')
    for i in range(0, numdevices):
        if (p.get_device_info_by_host_api_device_index(0, i).get('maxInputChannels')) > 0:
            infor = "Input Device id " + str(i) + " - " +  str(p.get_device_info_by_host_api_device_index(0, i).get('name')) + " - ch: " + str(p.get_device_info_by_host_api_device_index(0, i).get('maxInputChannels')) + " sr: " + str(p.get_device_info_by_host_api_device_index(0, i).get('defaultSampleRate'))
            
        if name in p.get_device_info_by_host_api_device_index(0, i).get('name'):
            log(str( name + " is found and will be used as an input device."))
            return i
    log( "There is no such a device named " + name)
    return -1

#print out results when voice is detected
def vad_result(task_outputs, predict_mode, file_name = None, logger = None, callback = None):
    if callback:
        data = {
            "status": "speaking",
            "analysis": task_outputs
        }
        callback(data)
    else:
        logs = ""
        for output in task_outputs:
            if predict_mode != 2:
                output_str = "\t" + str(output)
            else:
                output_str = ""
                for p in output:
                    output_str = output_str + "\t" + str(p) 
            logs += output_str
        
        if file_name:
            logs = file_name + logs    
        else:
            logs = logs[1:]
        log(logs, logger)

#print out results when voice is not detected
def no_vad_result(tasks, predict_mode, file_name = None, logger = None, callback = None):

    if callback:
        data = {
            "status": "silence"
        }
        callback(data)
    else:
        logs = ""
        for num_classes in tasks:
            if predict_mode != 2:
                output_str = '\t-1.'
            else:
                output_str = ""
                for p in range(num_classes):
                    output_str = output_str + "\t-1."
            logs += output_str
        
        if file_name:
            logs = file_name + logs    
        else:
            logs = logs[1:]

        log(logs, logger)

#predict frame by frame
def predict_frame(dec, frames, args, save = False):
    
    results = dec.predict(frames, feat_mode = args.feat_mode, feat_dim = args.feat_dim, three_d = args.three_d)
    
    if args.predict_mode == 0:
        task_outputs = dec.returnDiff(results)
    elif args.predict_mode == 1:
        task_outputs = dec.returnLabel(results)
    else:
        task_outputs = dec.returnClassDist(results)
    return task_outputs

#predict frames in a wave file
def predict_file(dec, pyaudio, path, frames, args, rate = 16000, format = pyaudio.paInt16, save = False):
    wf = wave.open(path, 'wb')
    wf.setnchannels(1)
    wf.setsampwidth(pyaudio.get_sample_size(format))
    wf.setframerate(rate)
    #this code works for only for pulseaudio
    #wf.writeframes(b''.join(frames))
    wf.writeframes(frames)
    wf.close()

    results = dec.predict_file(path, feat_mode = args.feat_mode, feat_dim = args.feat_dim, three_d = args.three_d)
    
    if save == False:
        os.remove(path)
    if args.predict_mode == 0:
        task_outputs = dec.returnDiff(results)
    elif args.predict_mode == 1:
        task_outputs = dec.returnLabel(results)
    else:
        task_outputs = dec.returnClassDist(results)
    return task_outputs

class LiveSer:
    def __init__(self): 
        print("Initializing LIVE_SER")

    def run(self,args):
        try:
            ser_thread = threading.Thread(name='ser', target=self.start, args=(args,))
            ser_thread.start()
        except KeyboardInterrupt:
            print("Stopping SER")
            ser_thread.join()


    def start(self,args):
        
        tasks_string = 'arousal:3,valence:3'
        tasks = [3,3]
        
        #audio device setup
        format = pyaudio.paInt16
        sample_rate = 16000
        frame_duration = 20
        n_channel = 1

        frame_len = int(sample_rate * (frame_duration / 1000.0))
        chunk = int(frame_len / n_channel)

        # vad mode, only accept [0|1|2|3], 0 more quiet 3 more noisy"
        vad_mode = 0

        logger = None
        
        log("frame_len: %d" % frame_len)
        log("chunk size: %d" % chunk)

        #feature extraction setting

        # the minimum length(ms) of speech for emotion detection
        vad_duration = 1000
        min_voice_frame_len = frame_len * (vad_duration / frame_duration)



        log("minimum voice frame length: %d" % min_voice_frame_len)

        # the minimum ratio of speech segments to the total segments
        speech_ratio = 0.3

        # the minimum energy of speech for emotion detection
        min_energy = 100

        feat_path = "./temp.csv"
        tmp_data_path = "./tmp"

        try:
            os.mkdir(tmp_data_path)
        except:
            log('Data folder already exists')
        

        #initialise vad
        vad = webrtcvad.Vad()
        vad.set_mode(vad_mode)

        #automatic gain normalisation
        if args.g_min and args.g_max:
            g_min_max = (args.g_min, args.g_max)
        else:
            g_min_max = (-0.45, 0.53)

        #initialise recognition model
        model_file = './emotion/model/si.ENG.cw.raw.2d.res.lstm.gpool.dnn.1.h5'

        # The provided model (./model/si.ENG.cw.raw.2d.res.lstm.gpool.dnn.1.h5) is trained by using end-to-end method, 
        # which means its input feature is raw-wave. So you have to specify -f_mode as "1". 
        # Also, the raw wave form has 16000 samples per sec. So we set -m_t_step as "16000".
        # The model uses 10 contextual windows; so each window has 1600 samples (-c_len 1600).

        context_len = 1600
        max_time_steps = 16000


        # predict_mode ("0 = diff, 1 = classification, 2 = distribution"), default = 2)
        args.predict_mode = 2

        # feat_mode'("0 = mspec, 1 = raw wav, 2 = lspec"), default = 0)
        args.feat_mode = 1


        # feat_dim, feature dimension (# spec for lspec or mspec"), default = 80)
        args.feat_dim = 80

        # 3DCNN
        args.three_d = False

        dec = Decoder(
                model_file = model_file, 
                elm_model_files = None, 
                context_len = context_len,
                max_time_steps = max_time_steps, 
                tasks = tasks_string,
                stl = False, 
                sr = sample_rate, 
                min_max = g_min_max, 
                seq2seq = False
        )

        self.feat_ext = dec.feat_ext
        
        log("Init audio")
        p = pyaudio.PyAudio()

        #open file (offline mode)

        log("no input wav file! Starting a live mode.")

        #open mic
        if args.device_id is None:
            args.device_id = find_device_id("pulse")
            if args.device_id == -1:
                log("There is no default device!, please check the configuration")
                sys.exit(-1)
                
            #open mic
        f = p.open(
                format = format, 
                channels = n_channel, 
                rate = sample_rate, 
                input = True, 
                input_device_index = args.device_id,
                frames_per_buffer = chunk
        )

        log("---Starting---")

        is_currently_speech = False
        total_frame_len = 0
        frames_16i = ''
        frames_np = []
        prev_task_outputs = None
        speech_frame_len = 0
        total_frame_count = 0


        file_path = None
        
        while True and not args.stop:
            #read a frame    
            data = f.read(chunk,exception_on_overflow=False)
                
            if data == '':
                break

            #check gain
            mx = audioop.max(data, 2)
            
            #VAD
            try:
                is_speech = vad.is_speech(data, sample_rate)
            except:
                log("end of speech")
                break

            if mx < min_energy:
                is_speech = 0
            
            #log(str('gain: %d, vad: %d' % (mx, is_speech)))   
            
            if is_speech == 1:
                speech_frame_len = speech_frame_len + chunk #note chunk is a half of frame length.

            frames_np.append(np.fromstring(data, dtype=np.int16))
            
            total_frame_len = total_frame_len + chunk

            #only if a sufficient number of frames are collected,
            if total_frame_len > min_voice_frame_len:       
                
                #only if the ratio of speech frames to the total frames is higher than the threshold
                if float(speech_frame_len)/total_frame_len > speech_ratio:
                    
                    #predict
                    frames_np = np.hstack(frames_np)
                    outputs = predict_frame(dec, frames_np, args)
                    
                    vad_result(outputs, args.predict_mode, file_path, logger, args.callback)
                else:
                    no_vad_result(tasks, args.predict_mode, "", logger, args.callback)
                
                #initialise variables
                total_frame_len = 0
                speech_frame_len = 0
                frames_16i = ''
                frames_np = []

            total_frame_count = total_frame_count + 1
            if total_frame_count % 100 == 0:
                log(str("total frame: %d" %( total_frame_count)))

        log("---done---")
        p.terminate()   

if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    
    parser.add_argument("-d_id", "--device_id", dest= 'device_id', type=int, help="a device id for microphone", default=None)
    
    #automatic gain normalisation
    parser.add_argument("-g_min", "--gain_min", dest= 'g_min', type=float, help="the min value of automatic gain normalisation")
    parser.add_argument("-g_max", "--gain_max", dest= 'g_max', type=float, help="the max value of automatic gain normalisation")

    args = parser.parse_args()

    if len(sys.argv) == 1:
        parser.print_help()
        listup_devices()       
        sys.exit(1)
    
    print("args: " + str(args))
    ser(args)
