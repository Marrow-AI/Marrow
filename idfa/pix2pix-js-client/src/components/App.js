import React, { Component } from 'react';
import VideoPlayer from './VideoPlayer';
// import Subtitle from './Subtitle';
import Fader from './Fader';
import CinemaMode from './CinemaMode';
import GUI from './GUI';
import Canvas from "./Canvas";
import { AppContext } from './Provider';
import '../styles/App.css';

class App extends Component {
  componentDidMount(){
    const { setCameraDevices, initCamera, HDConfigs, serverIP, pix2pixPort, gotModelSetInServer } = this.props.context;
    navigator.mediaDevices.enumerateDevices()
      .then(d => {
        const devices = d.filter((device) => (device.kind === 'videoinput'));
        const camera = devices[0].deviceId;
        const constrains = {...HDConfigs}
        constrains.video.deviceId = { exact: camera }
        initCamera(constrains);
        setCameraDevices(devices);
      })
    //Get the current scene
    fetch(`http://${serverIP}:${pix2pixPort}/current_model`)
      .then(r => r.json())
      .then(r => gotModelSetInServer(r.current_model))
  }

  handleKeyPress = (e) => {
    const { debugMode, changeDebugMode } = this.props.context;
    if(e.key === 'd') {
      changeDebugMode(!debugMode);
    }
  }

  render() {
    const { debugMode } = this.props.context;
    return <div className="App" tabIndex="0" onKeyDown={this.handleKeyPress}>
        <Canvas />
        <VideoPlayer type='camera' />
        <VideoPlayer type='scene' />
        <CinemaMode />
        {/* <Subtitle /> */}
        <Fader />
        {debugMode ? <GUI /> : null}
      </div>;
  }
}

export default props => (
  <AppContext.Consumer>
    {context => <App {...props} context={context} />}
  </AppContext.Consumer>
);


