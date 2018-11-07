import React, { Component } from 'react';
import * as dg from 'dis-gui';
import { withContext } from './Provider';

class GUI extends Component {
  changeVideoResolution = (value) => {
    const { currentConstrains, initCamera, notHDConfigs, HDConfigs, isScenePlaying, updateScenePlayingStatus } = this.props.context;
    let constrains = notHDConfigs;
    if (value) {
      constrains = HDConfigs;
    }
    constrains.video.deviceId.exact = currentConstrains.video.deviceId.exact;
    initCamera(constrains);
    if (isScenePlaying) {
      updateScenePlayingStatus(false);
    }
  }

  changeCameraSource = (label) => {
    const { cameras, initCamera, currentConstrains, updateScenePlayingStatus, isScenePlaying } = this.props.context;
    const camera = cameras.find(c => c.label === label);
    const constrains = currentConstrains;
    constrains.video.deviceId = { exact: camera.deviceId }
    initCamera(constrains);
    if (isScenePlaying) {
      updateScenePlayingStatus(false);
    }
  }

  setTheScene = (name) => {
    const { scenes, setCurrentScene } = this.props.context;
    const scene = scenes.find(c => c.name === name);
    setCurrentScene(scene);
  }

  playOrStopScene = () => {
    const { scenes, currentScene, updateScenePlayingStatus, isScenePlaying, initScene } = this.props.context;
    const scene = scenes.find(c => c.name === currentScene.name);
    if (isScenePlaying) {
      document.getElementById('sceneElement').pause();
    } else {
      initScene(scene.src);
    }
    updateScenePlayingStatus(!isScenePlaying);
  } 

  render() {
    const { 
      cameras, 
      scenes, 
      setCinemaModeSize, 
      isScenePlaying,
      isShowingVideoCanvas,
      setVideoCanvasWidth,
      setVideoCanvasHeight,
      setShowingVideoCanvas,
      setPix2pixCanvasWidth,
      setPix2pixCanvasHeight,
      setShowingPix2pixCanvas,
      isShowingPix2pixCanvas,
      isConnectedToServer,
      serverIP, 
      denseposePort,
      pix2pixPort,
       queryRoute,
      isConnectedToMarrow,
      marrowIP,
      marrowPort,
      marrowRoute,
      isSendingFrames,
      updateSendingFrameStatus,
      setServerIP,
      setDenseposePort,
      setPix2pixPort,
      connectToServer,
      setMarrowIP,
      setMarrowPort,
      connectToMarrow,
      autoplayOn,
      autoplaySceneDuration,
      setModel
    } = this.props.context;
  
    return (
      <div className='GUI'>
      <dg.GUI>
        <dg.Select 
          label='Camera' 
          options={cameras.map(c => c.label)}
          onChange={(v) => this.changeCameraSource(v)}
        />
        <dg.Checkbox 
          label='HD' 
          checked={true}
          onChange={(v) => this.changeVideoResolution(v)}
        />
        <dg.Number 
          label='Cinema Mode' 
          value={150} 
          min={0} 
          max={500} 
          step={1}
          onChange={(s) => setCinemaModeSize(s)}
        />
        <dg.Text label='Model in Server' value={this.props.context.modelInServer}/>
        <dg.Select
          label='Model'
          options={scenes.map(c => c.name)}
          onChange={(n) => setModel(n)}
        />
        <dg.Select 
          label='Scene' 
          options={scenes.map(c => c.name)}
          onChange={(n) => this.setTheScene(n)}
        />
        <dg.Button 
          label={isScenePlaying ? 'Stop' : 'Play Scene'}
          onClick={this.playOrStopScene}
        />
        <dg.Folder label='Video Canvas' expanded={false}>
          <dg.Number 
            label='Width' 
            value={256} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => setVideoCanvasWidth(s)}
          />
          <dg.Number
            label='Height' 
            value={128} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => setVideoCanvasHeight(s)}
          />
          <dg.Button 
            label={isShowingVideoCanvas ? 'Hide Canvas' : 'Show Canvas'}
            onClick={() => setShowingVideoCanvas(!isShowingVideoCanvas)}
          />
        </dg.Folder>
        <dg.Folder label='Pix2Pix Canvas' expanded={false}>
          <dg.Number 
            label=' Width' 
            value={1280} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => setPix2pixCanvasWidth(s)}
          />
          <dg.Number 
            label='Height' 
            value={720} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => setPix2pixCanvasHeight(s)}
          />
          <dg.Button 
            label={isShowingPix2pixCanvas ? 'Hide Canvas' : 'Show Canvas'}
            onClick={() => setShowingPix2pixCanvas(!isShowingPix2pixCanvas)}
          />
        </dg.Folder>
        <dg.Folder label='Server' expanded={true}>
          <dg.Text label='Server Connection' value={isConnectedToServer ? 'Connected' : 'Not Connected'}/>
          <dg.Text label='IP' 
            value={serverIP}
            onChange={(value) => setServerIP(value)}
          />
          <dg.Text label='Pix2Pix Port' 
            value={pix2pixPort}
            onChange={(value) => setPix2pixPort(value)}
          />
          <dg.Text label='DensePose Port' 
            value={denseposePort}
            onChange={(value) => setDenseposePort(value)}
          />
          <dg.Button 
            label={isConnectedToServer ? 'Disconnect' : 'Connect to Server'}
            onClick={() => connectToServer(serverIP, denseposePort, queryRoute)}
          />
          <dg.Button 
            label={isSendingFrames ? 'Stop Sending Frames' : 'Start Sending Frames'}
            onClick={() => updateSendingFrameStatus(!isSendingFrames)}
          />
        </dg.Folder>
        <dg.Folder label='Marrow' expanded={true}>
          <dg.Text label='Server Connection' value={isConnectedToMarrow ? 'Connected' : 'Not Connected'}/>
          <dg.Text label='IP' 
            value={marrowIP}
            onChange={(value) => setMarrowIP(value)}
          />
          <dg.Text label='Marrow Port' 
            value={marrowPort}
            onChange={(value) => setMarrowPort(value)}
          />
          <dg.Button 
            label={isConnectedToServer ? 'Disconnect' : 'Connect to Server'}
            onClick={() => connectToMarrow(marrowIP, marrowPort, marrowRoute)}
          />
        </dg.Folder>
        <dg.Folder label='Transitions' expanded={true}>
          <dg.Checkbox
            label='Autoplay'
            checked={autoplayOn}
            onChange={() => this.props.context.startAutoplay()}
          />
          <dg.Number
            label='Scene Duration'
            value={autoplaySceneDuration}
            min={1}
            max={100}
            step={1}
            onChange={(v) => this.props.context.setAutoplayDuration('scene', v)}
          />
        </dg.Folder>
      </dg.GUI>
      </div>
    );
  }
}

export default withContext(GUI);



