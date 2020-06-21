# GAN : Latent Space Explorer 

## Installation
Create a miniconda environment:
```
$ conda create --name <env> --file requirements.txt
```
Symlink dnnlib:
```
$ ln -sf <path_to_stylegan>/dnnlib .
```
Server will start listening at port 8080. Animation definitions are stored in the _animations_ subfolder. StyleGAN Snapshots folder is defined on this line:
```
    url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-{}.pkl".format(snapshot))
```
Available snapshots are hard-coded in **templates/index.html**:  
```
const SNAPSHOTS = [
    ..
]
```
## Command line arguments
### Dummy mode
If you just want to test the interface without running an actual GAN:
```
python marrow_explore.py --dummy
``` 

## Code walkthroughs
### Generating transitions with StyleGAN and Numpy
Images are generated from one-dimensional Z-vectors (‘noise’ vectors) of 512 numbers. Initializing random Z-vectors is easy with Numpy:
```
self.rnd = np.random.RandomState()
self.latent_source = self.rnd.randn(512)[None, :]
self.latent_dest = self.rnd.randn(512)[None, :]
Once we have a source and destination for the transition, interpolations are done using Numpy’s linespace function:
self.steps = int(args['steps'])
self.linespaces = np.linspace(0, 1, self.steps)
self.linespace_i = 0
```
To generate the image using StyleGAN, we simply apply the linspace to our source and destination vectors in the following manner:
```
self.latents = (self.linespaces[self.linespace_i] * self.latent_dest + (1-self.linespaces[self.linespace_i]) * self.latent_source)
images = self.Gs.run(self.latents, None, truncation_psi=0.7, randomize_noise=False, output_transform=self.fmt)
image = images[0]
Note the randomize_noise=False argument. If we were to set it to true, we would still have some random noise added to every image. While this may work well to simulate a more organic output, it doesn’t suit our purpose of matching with a pre-baked animation.
Saving and loading Z-vectors is also very easy with Numpy:
with open('animations/{}/source.npy'.format(args['name']), 'wb+') as source_file:
    np.save(source_file, self.latent_source)
... 
# Loading
self.latent_source = np.load('animations/{}/source.npy'.format(args['animation']))
```
### Communicating between a Flask web server and a TensorFlow session thread
A TensorFlow session has to run in its own thread, independently of the web server. However, insofar as the web functions have to wait for GAN’s output before returning to the browser, I needed a mechanism for synchronization. I opted to use a thread-safe queue to send requests from Flask’s web function to the GAN host, and asyncio futures as a low-level signaling mechanism between GAN and the web functions.
The GAN thread loops indefinitely while waiting for queue requests, and it is aware of the main asyncio loop that is running in the background. When Flask gets a generate request, it puts a new message in GAN’s queue, along with a new future object that is used as the done callback:
```
@app.route('/generate')
def generate():
    future = loop.create_future()
    q.put((future, "generate", request.args))
    data = loop.run_until_complete(future)
    return jsonify(result=data)
```
The GAN thread picks up the message, generates the image and sets the future as done when it is ready:
```
(future,request,args) = self.queue.get()
if request == "generate":
    ...  # Generate the image into a b64text
    self.loop.call_soon_threadsafe(
        future.set_result, b64text
    )
```
One special handling is needed when a new snapshot is selected. I found that I had to close the TensorFlow session, join the thread, and restart it with the new snapshot:
```
# At the GAN thread
if args['snapshot'] != self.current_snapshot:
    self.current_snapshot = args['snapshot']
    tf.get_default_session().close()
    tf.reset_default_graph()
    break
# At the web server
global gan
gan.join()
args.snapshot = params['snapshot']
gan = Gan(q, loop, args)
gan.start()
```
