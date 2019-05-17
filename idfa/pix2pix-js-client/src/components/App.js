import React, { Component } from 'react'
import VideoPlayer from './VideoPlayer';
import ImagesSlider from './ImagesSlider';
import CenterImage from './CenterImage';
import Fader from './Fader';
import CinemaMode from './CinemaMode';
import GUI from './GUI';
import Canvas from "./Canvas";
import { AppContext } from './Provider';
import '../styles/App.css';

class App extends Component {
  componentDidMount(){
    const { context } = this.props;
    context.connectToServer(context.serverIP);
    context.connectToMarrow(context.marrowIP, context.marrowPort, context.marrowRoute);
    navigator.mediaDevices.enumerateDevices()
      .then(d => {
        const devices = d.filter((device) => (device.kind === 'videoinput'));
        const camera = devices[1].deviceId; // this is just an hard code to choose a device in the nfb
        const constrains = {...context.HDConfigs}
        constrains.video.deviceId = { exact: camera }
        context.initCamera(constrains);
        context.setCameraDevices(devices);
      })

  }

  handleKeyPress = (e) => {
    const { context } = this.props;
    if(e.key === 'd') {
      context.changeDebugMode(!context.debugMode);
    }
  }

  render() {
    const { debugMode } = this.props.context;
    return (

        <div className="App" id="App" ref='App' tabIndex="0" onKeyDown={this.handleKeyPress}>
          <CenterImage />
          <ImagesSlider />
          <Canvas />
          <VideoPlayer type='camera' />
          <CinemaMode />
          <Fader />
          {debugMode ? <GUI /> : null}
        </div>

    );
  }
}

export default props => (
  <AppContext.Consumer>
    {context => <App {...props} context={context} />}
  </AppContext.Consumer>
);
