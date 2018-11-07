import React, {Component} from 'react';
import { withContext } from './Provider';
import '../styles/Canvas.css';

class Canvas extends Component {
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
      <div className="Canvas">
        <canvas
          width={context.cameraCanvasWidth}
          height={context.cameraCanvasHeight}
          id="cameraCanvas"
          style={{
            display: cameraCanvasDisplay
          }}
        />
        <canvas 
          width={context.pix2pixCanvasWidth} 
          height={context.pix2pixCanvasHeight} 
          id="pix2pixCanvas"
          style={{             
            display: pix2pixCanvasDisplay
          }}
        />
      </div>
    );
  }
}

export default withContext(Canvas);

