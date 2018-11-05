import React, { Component } from 'react';
import io from 'socket.io-client';
const AppContext = React.createContext()

// Server IP
const IP = '0.0.0.0';

class AppProvider extends Component {
  state = {
    scenes: [],
    currentScene: null,
    setCurrentScene: (s) => this.setState({currentScene: s}),
    isScenePlaying: false,
    updateScenePlayingStatus: (s) => this.setState({isScenePlaying: s}),
    debugMode: true,
    changeDebugMode: (s) => this.setState({debugMode: s}),
    cameras: [],
    setCameraDevices: (c) => this.setState({cameras: c}),
    HDConfigs: {
      video: {width: {min: 1080}, height: {min: 720}, deviceId: {}}
    },
    notHDConfigs: {
      video: {width: {exact: 640}, height: {exact: 480}, deviceId: {}}
    },
    cinemaModeSize: 150,
    setCinemaModeSize: (s) => this.setState({cinemaModeSize: s}),
    srcObject: null,
    currentConstrains: null,
    currentSceneIndex: 0,
    setCurrentSceneIndex: (s) => this.setState({ currentSceneIndex: s }),
    interval: null,
    startAutoplay: () => {
      if (this.state.interval){
        clearInterval(this.state.interval);
        clearInterval(this.state.faderInterval);
        this.setState({ interval: null,  faderInterval: null });
        document.getElementById('sceneElement').pause();
      } else {
        const loop = () => {
          setTimeout(() => this.setState({ faderOpacity: 1 }), (this.state.autoplaySceneDuration - 1.5) * 1000);
          // this.state.initScene(SCENES[this.state.currentSceneIndex].src);
          // this.state.setModel(SCENES[this.state.currentSceneIndex].name);
          // const currentSceneIndex = (this.state.currentSceneIndex + 1) % SCENES.length;
          // this.state.setCurrentSceneIndex(currentSceneIndex);
        }
        loop();
        const interval = setInterval(() => loop(), this.state.autoplaySceneDuration * 1000);
        this.setState({ interval })
      }
    },
    opacities: {
      scene: 1,
      camera: 0,
      transfer: 1,
    },
    faderOpacity: 0,
    initCamera: (constrains) => {
      const videoElement = document.getElementById('cameraElement');
      if (this.state.srcObject && this.state.srcObject.getTracks) {
        this.state.srcObject.getTracks().forEach(track => track.stop());
      }
      navigator.mediaDevices        
        .getUserMedia(constrains)
        .then((stream) => {
          videoElement.srcObject = stream;
          this.setState({ srcObject: stream })
          this.setState({ currentConstrains: constrains })
        })
        .catch((error) => {
          console.log("Something went wrong with the camera!", error);
        });
    },
    initScene: (scene) => {
      const videoElement = document.getElementById('sceneElement');
      videoElement.srcObject = null;
      videoElement.src = scene;
      videoElement.loop = true;
      videoElement.play();
      this.setState({ srcObject: scene });
    },
    videoCanvasWidth: 256,
    videoCanvasHeight: 128,
    setVideoCanvasWidth: (s) => this.setState({videoCanvasWidth: s}),
    setVideoCanvasHeight: (s) => this.setState({videoCanvasHeight: s}),
    isShowingVideoCanvas: true,
    setShowingVideoCanvas: (s) => this.setState({isShowingVideoCanvas: s}),
    pix2pixCanvasWidth: 1080,
    pix2pixCanvasHeight: 720,
    setPix2pixCanvasWidth: (s) => this.setState({pix2pixCanvasWidth: s}),
    setPix2pixCanvasHeight: (s) => this.setState({pix2pixCanvasHeight: s}),
    isShowingPix2pixCanvas: true,
    setShowingPix2pixCanvas: (s) => this.setState({isShowingPix2pixCanvas: s}),
    autoplayOn: false,
    autoplaySceneDuration: 10,
    autoplayCameraDuration: 3,
    autoplayTransferDuration: 10,
    setAutoplayDuration: (k, v) => {
      switch (k) {
        case "scene":
          this.setState({ autoplaySceneDuration: v });
          break;

        case "camera":
          this.setState({ autoplayCameraDuration: v });
          break;

        case "transfer":
          this.setState({ autoplayTransferDuration: v });
          break;
      }
    },
    setModel: async (name) => {
      return fetch(`http://${this.state.serverIP}:${this.state.pix2pixPort}/switch_model`, {
        body: JSON.stringify({ model: name }),
        method: "POST",
        headers: {
          "content-type": "application/json"
        }
      })
      .then(r => r.json())
      .then(r => { 
        this.setState({ faderOpacity: 0 });
        this.state.gotModelSetInServer(r.current_model);
      })
    },
    modelInServer: '',
    gotModelSetInServer: (name) => this.setState({ modelInServer: name }),
    serverIP: IP, 
    denseposePort: '22100',
    pix2pixPort: '23100',
    queryRoute: '/query',
    isConnectedToServer: false,
    isSendingFrames: false,
    socket: null,
    setServerIP: (ip) => this.setState({serverIP: ip}), 
    setDenseposePort: (port) => this.setState({denseposePort: port}), 
    setPix2pixPort: (port) => this.setState({pix2pixPort: port}), 
    connectToServer: (ip, port, route) => {
      if (!this.state.socket) {
        const socket = io(`http://${ip}:${port}${route}`);
        socket.on('connect', () => {
          this.setState({ isConnectedToServer: true });
        });
        socket.on('disconnect', () => {
          this.setState({ isConnectedToServer: false });
        });
        socket.on('update_response', (data) => {
          const canvas = document.getElementById('pix2pixCanvas');
          const ctx = canvas.getContext('2d');
          const img = new Image();
          img.onload = () => {
            ctx.drawImage(img, 0, 0, this.state.pix2pixCanvasWidth, this.state.pix2pixCanvasHeight);
            if (this.state.isSendingFrames) {
              this.state.sendFrames();
            }
          };
          img.onerror = () => {
            if (this.state.isSendingFrames) {
              this.state.sendFrames();
            }
          }
          img.src = "data:image/jpg;base64," + data.results;
        });
        this.setState({ socket: socket });
      } else {
        this.state.socket.disconnect();
        this.setState({ socket: null });
      }
    },
    updateSendingFrameStatus: (state) => {
      if (state) {
        this.setState({ isSendingFrames: true });
        this.state.sendFrames();
      } else {
        this.setState({ isSendingFrames: false });
      }
    },
    sendFrames: () => {
      if (this.state.isConnectedToServer) {
        this.setState({ isSendingFrames: true });
        const canvas = document.getElementById('videoCanvas');
        this.state.socket.emit('update_request', {
          data: canvas.toDataURL('image/jpg')
        });
      }
    }
  }
  
  render() {
    return (
    <AppContext.Provider value={this.state}>
      {this.props.children}
    </AppContext.Provider>)
  }
}

const withContext = (ReactComponent) => (
  props => (
    <AppContext.Consumer>
      {context => <ReactComponent {...props} context={context} />}
    </AppContext.Consumer>
  )
);

export {
  AppContext,
  AppProvider,
  withContext
};