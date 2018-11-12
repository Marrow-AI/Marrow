import React, {Component} from 'react';
import * as posenet from '@tensorflow-models/posenet';
import { withContext } from './Provider';
import '../styles/Canvas.css';

class Canvas extends Component {
  constructor(){
    super();
    this.state = {
      peopleDetected: 0,
      net: null
    }
  }
  
  componentDidMount() {
    this.loadPoseNet();
    window.requestAnimationFrame(this.renderVideoIntoCanvas);
  }

  loadPoseNet = () => {
    posenet.load()
      .then(net => {
        this.setState({ net })
    })
  }

  renderVideoIntoCanvas = () => {
    const { context } = this.props;
    const { net } = this.state;
    const video = document.getElementById('cameraElement');
    const canvas = document.getElementById('cameraCanvas');
    const ctx = canvas.getContext('2d');
    ctx.drawImage(video, 0, 0, context.cameraCanvasWidth, context.cameraCanvasHeight);
    if (net) {
      // net.estimateMultiplePoses(canvas, 0.5, false, 16, 4)
      // .then(r => {
      //   if(r.length > 2) {
      //     context.sendFrames();
      //   }
      // })
    }
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
            display: pix2pixCanvasDisplay,
            opacity: context.isSliding ? 0 : 1,
            transition: `all 30s`
          }}
        />
      </div>
    );
  }
}

export default withContext(Canvas);

