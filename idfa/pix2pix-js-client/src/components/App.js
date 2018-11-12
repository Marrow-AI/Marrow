import React, { Component } from 'react'
import VideoPlayer from './VideoPlayer';
import ImagesSlider from './ImagesSlider';
import CenterImage from './CenterImage';
import Fader from './Fader';
import CinemaMode from './CinemaMode';
import GUI from './GUI';
import Canvas from "./Canvas";
import { AppContext } from './Provider';
import ReactDOM from 'react-dom';
import '../styles/App.css';

class App extends Component {
  componentDidMount(){
    const { context } = this.props;
    navigator.mediaDevices.enumerateDevices()
      .then(d => {
        const devices = d.filter((device) => (device.kind === 'videoinput'));
        const camera = devices[0].deviceId;
        const constrains = {...context.HDConfigs}
        constrains.video.deviceId = { exact: camera }
        context.initCamera(constrains);
        context.setCameraDevices(devices);
      })

      const maptastic = document.createElement("script");
      maptastic.src = "http://localhost:3000/maptastic.min.js";
      // script.async = true;
      document.body.appendChild(maptastic);
      
      setTimeout(() => {
        const projection = document.createElement("script");
        projection.src = "http://localhost:3000/projection.js";
        // script.async = true;
        document.body.appendChild(projection);
      }, 500)
      window.app = ReactDOM.findDOMNode(this.refs['App']).getBoundingClientRect();
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


