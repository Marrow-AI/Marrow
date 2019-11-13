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
    const { context } = this.props;
  
    return (
      <div className='GUI'>
      <dg.GUI>
        <dg.Select 
          label='Camera' 
          options={context.cameras.map(c => c.label)}
          onChange={(v) => this.changeCameraSource(v)}
        />
        <dg.Folder label='Camera' expanded={false}>
          <dg.Number 
            label='Width' 
            value={400} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => context.setCameraCanvasWidth(s)}
          />
          <dg.Number
            label='Height' 
            value={300} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => context.setCameraCanvasHeight(s)}
          />
          <dg.Button 
            label={context.isShowingCameraCanvas ? 'Hide Camera' : 'Show Camera'}
            onClick={() => context.setShowingCameraCanvas(!context.isShowingCameraCanvas)}
          />
        </dg.Folder>
        <dg.Folder label='Pix2Pix' expanded={false}>
          <dg.Number 
            label='Width' 
            value={1280} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => context.setPix2pixCanvasWidth(s)}
          />
          <dg.Number 
            label='Height' 
            value={720} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => context.setPix2pixCanvasHeight(s)}
          />
          <dg.Button 
            label={context.isShowingPix2pixCanvas ? 'Hide Canvas' : 'Show Canvas'}
            onClick={() => context.setShowingPix2pixCanvas(!context.isShowingPix2pixCanvas)}
          />
        </dg.Folder>
        <dg.Folder label='Server' expanded={false}>
          <dg.Text label='Server Connection' value={context.isConnectedToServer ? 'Connected' : 'Not Connected'}/>
          <dg.Text label='IP' 
            value={context.serverIP}
            onChange={(value) => context.setServerIP(value)}
          />
          <dg.Button 
            label={context.isConnectedToServer ? 'Disconnect' : 'Connect to Server'}
            onClick={() => context.connectToServer(context.serverIP)}
          />
          <dg.Button 
            label={context.isSendingFrames ? 'Stop Sending Frames' : 'Start Sending Frames'}
            onClick={() => context.updateSendingFrameStatus(!context.isSendingFrames)}
          />
        </dg.Folder>
        <dg.Folder label='Marrow' expanded={true}>
          <dg.Text label='Server Connection' value={context.isConnectedToMarrow ? 'Connected' : 'Not Connected'}/>
          <dg.Text label='IP' 
            value={context.marrowIP}
            onChange={(value) => context.setMarrowIP(value)}
          />
          <dg.Text label='Marrow Port' 
            value={context.marrowPort}
            onChange={(value) => context.setMarrowPort(value)}
          />
          <dg.Button 
            label={context.isConnectedToMarrow ? 'Disconnect' : 'Connect to Server'}
            onClick={() => context.connectToMarrow(context.marrowIP, context.marrowPort, context.marrowRoute)}
          />
          { context.isConnectedToMarrow &&
              <dg.Button 
                label='Send fake pix2pix updates'
                  onClick={() => context.sendFakePix2Pix()}
              />
          }
        </dg.Folder>
        <dg.Folder label='Images' expanded={false}>
          <dg.Number 
            label='Width' 
            value={220} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => context.setImagesWidth(s)}
          />
          <dg.Number 
            label='Height' 
            value={133} 
            min={0} 
            max={2000} 
            step={1}
            onChange={(s) => context.setImagesHeight(s)}
          />
          <dg.Number 
            label='Amount of images' 
            value={10} 
            min={0} 
            max={200} 
            step={1}
            onChange={(s) => context.setAmountOfImages(s)}
          />
          <dg.Number 
            label='Transition Speed' 
            value={2} 
            min={0} 
            max={100} 
            step={1}
            onChange={(s) => context.setSliderSpeed(s)}
          />
          <dg.Button 
            label={context.isSliding ? 'Stop and Center' : 'Start Sliding'}
            onClick={() => context.setIsSliding(!context.isSliding)}
          />
        </dg.Folder>    
        <dg.Button 
          label={context.isExperienceRunning ? 'Stop Experience' : 'Start Experience'}
          onClick={() => context.setExperienceStatus(!context.isExperienceRunning)}
        />
      </dg.GUI>
      </div>
    );
  }
}

export default withContext(GUI);



