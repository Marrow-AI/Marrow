import React, {Component} from 'react';
import { withContext } from './Provider';
import '../styles/Canvas.css';

class Canvas extends Component {

  componentDidMount() {
    window.requestAnimationFrame(this.renderVideoIntoCanvas);
  }

  renderVideoIntoCanvas = () => {
    const { videoCanvasWidth, videoCanvasHeight } = this.props.context;
    const video = document.getElementById('cameraElement');
    const canvas = document.getElementById('videoCanvas');
    const ctx = canvas.getContext('2d');
    ctx.drawImage(video, 0, 0, videoCanvasWidth, videoCanvasHeight);
    window.requestAnimationFrame(this.renderVideoIntoCanvas);
  }

  render() {
    const { 
      videoCanvasWidth, 
      videoCanvasHeight,
      isShowingVideoCanvas,
      pix2pixCanvasWidth,
      pix2pixCanvasHeight,
      isShowingPix2pixCanvas,
      opacities
    } = this.props.context;

    let videoCanvasDisplay = 'none';
    if (isShowingVideoCanvas) {
      videoCanvasDisplay = 'inline';
    }

    let pix2pixCanvasDisplay = 'none';
    if (isShowingPix2pixCanvas) {
      pix2pixCanvasDisplay = 'inline';
    }

    return (
      <div className="Canvas">
        <canvas
          width={videoCanvasWidth}
          height={videoCanvasHeight}
          id="videoCanvas"
          style={{
            opacity: opacities['transfer'],
            display: videoCanvasDisplay
          }}
        />
        <canvas 
          width={pix2pixCanvasWidth} 
          height={pix2pixCanvasHeight} 
          id="pix2pixCanvas"
          style={{ 
            opacity: opacities['transfer'],
            display: pix2pixCanvasDisplay
          }}
        />
      </div>
    );
  }
}

export default withContext(Canvas);

