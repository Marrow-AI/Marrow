import React, { Component } from 'react';
import io from 'socket.io-client';
const AppContext = React.createContext()

// Server IP
const IP = 'https://pix2pix.api.marrow.raycaster.studio/generate';

class AppProvider extends Component {
  state = { 
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
    interval: null,
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
    cameraCanvasWidth: 256,
    cameraCanvasHeight: 128,
    setCameraCanvasWidth: (s) => this.setState({cameraCanvasWidth: s}),
    setCameraCanvasHeight: (s) => this.setState({cameraCanvasHeight: s}),
    isShowingCamera: true,
    setShowingCamera: (s) => this.setState({isShowingCamera: s}),
    isShowingCameraCanvas: true,
    setShowingCameraCanvas: (s) => this.setState({isShowingCameraCanvas: s}),
    pix2pixCanvasWidth: 1080,
    pix2pixCanvasHeight: 720,
    setPix2pixCanvasWidth: (s) => this.setState({pix2pixCanvasWidth: s}),
    setPix2pixCanvasHeight: (s) => this.setState({pix2pixCanvasHeight: s}),
    isShowingPix2pixCanvas: true,
    setShowingPix2pixCanvas: (s) => this.setState({isShowingPix2pixCanvas: s}),
    autoplayOn: false,
    autoplayCameraDuration: 3,
    serverIP: IP,
    isConnectedToServer: false,
    isSendingFrames: false,
    socket: null,
    setServerIP: (ip) => this.setState({serverIP: ip}),
    imageSliderOpacity: 1,
    setImageSliderOpacity: (v) => this.setState({imageSliderOpacity: v}),
    imagesWidth: 220,
    imagesHeight: 133,
    amountOfImages: 10,
    setAmountOfImages: (v) => this.setState({amountOfImages: v}),
    setImagesWidth: (v) => this.setState({imagesWidth: v}),
    setImagesHeight: (v) => this.setState({imagesHeight: v}),
    connectToServer: (ip, port, route) => {
      if (!this.state.socket) {
        const socket = io(ip);
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
        const canvas = document.getElementById('cameraCanvas');
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
