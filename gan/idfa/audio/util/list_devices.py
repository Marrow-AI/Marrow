import pyaudio

p = pyaudio.PyAudio()
    
print("HOST APIs")

for i in range(p.get_host_api_count()):
    print(p.get_host_api_info_by_index(i))

print("----------")

print("DEVICES")
print("-----------")

for i in range(p.get_device_count()):
    info = p.get_device_info_by_index(i)
   # if (info['hostApi'] == 1):
    print(info)
