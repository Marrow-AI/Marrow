import React, {Component} from 'react';
import { withContext } from './Provider';
import '../styles/Canvas.css';

class Canvas extends Component {
  constructor(){
    super();
    this.state = {
      peopleDetected: 0
    }
  }
  
  componentDidMount() {
    window.requestAnimationFrame(this.renderVideoIntoCanvas);
  }

  renderVideoIntoCanvas = () => {
    const { context } = this.props;
    const video = document.getElementById('cameraElement');
    const canvas = document.getElementById('cameraCanvas');
    const ctx = canvas.getContext('2d');
    ctx.drawImage(video, 0, 0, context.cameraCanvasWidth, context.cameraCanvasHeight);
    window.requestAnimationFrame(this.renderVideoIntoCanvas);
  }
  
  render() {
    const { context } = this.props;

    let cameraCanvasDisplay = 'none';
    if (context.isShowingCameraCanvas) {
      cameraCanvasDisplay = 'inline';
    }

    let pix2pixCanvasDisplay = 'none';
    if (context.isShowingPix2pixCanvas) {
      pix2pixCanvasDisplay = 'inline';
    }
    return (
      <div 
        className="Canvas"
        style={{
          opacity: context.hide ? 0 : 1 // 0 : 1
        }}  
      >
        <canvas
          width={context.cameraCanvasWidth}
          height={context.cameraCanvasHeight}
          id="cameraCanvas"
          style={{
            display: cameraCanvasDisplay
          }}
        />
        <div
          id="pix2pixContainer"
          width={context.pix2pixCanvasWidth} 
          height={context.pix2pixCanvasHeight} 
          style={{
            opacity: context.showPix2Pix ? 1 : 0, // 1 : 0
            transition: context.showPix2Pix ? 'opacity 20s' : 'opacity 0s'  // 0 : 1
          }}
        >
          <canvas 
            width={context.pix2pixCanvasWidth} 
            height={context.pix2pixCanvasHeight} 
            id="pix2pixCanvas"
            style={{             
              display: pix2pixCanvasDisplay,
              opacity: context.isSliding ? 0 : 1  // 0 : 1
            }}
          />
        </div>
      </div>
    );
  }
}

export default withContext(Canvas);

