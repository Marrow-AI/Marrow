import React, { Component } from 'react';
import ReactDOM from 'react-dom';
import * as posenet from '@tensorflow-models/posenet';
import { withContext } from './Provider';
import '../styles/ImageSlider.css';


const BASE_URL = 'http://localhost:3000'
const WIDTH = 228;
const TIME = 300;

class ImageSlider extends Component {
  constructor(props){
    super(props);
    this.state = {
      imgInCenter: this.props.context.amountOfImages - 1,
      xTranslate: -this.props.context.amountOfImages*WIDTH,
      transitionTime: 0,
      net: null
    }
  }
 
  componentDidMount(){
    this.loadPoseNet();
    this.setState({
      xTranslate: window.innerWidth,
      transitionTime: TIME
    });
    

    setInterval(() => {
      const { context } = this.props;
      const { net } = this.state;
      const pos = ReactDOM.findDOMNode(this.refs['ImageSlider']).getBoundingClientRect();
      
      for (let i = 0; i < context.amountOfImages + 1; i++){
        const el = ReactDOM.findDOMNode(this.refs[`IMAGE_${i}`]);
        if (el) {
          const { right } = el.getBoundingClientRect();
          if (right > window.innerWidth/2 - 110 && right < window.innerWidth/2 + 110) {
            if(context.isSliding) {
              context.setCenterImage(i);
            }
          }
        }
      }

      if(!context.isSliding) {
        const center = ReactDOM.findDOMNode(this.refs[`IMAGE_${context.centerImage}`]).getBoundingClientRect();
        let deltaAlignToCenter = window.innerWidth/2 - center.left - 110;

        if (center.left > window.innerWidth/2 + 110) {
          deltaAlignToCenter = deltaAlignToCenter * -1;
        } 
        this.setState({ 
          xTranslate: pos.left + deltaAlignToCenter,
          transitionTime: 8
        });

      } else {
        if (context.centerImage === 1) {
          this.setState({ 
            xTranslate: -this.props.context.amountOfImages*WIDTH,
            transitionTime: 0
          });
          context.setCenterImage(49);
        } else {
          this.setState({ 
            xTranslate: window.innerWidth,
            transitionTime: TIME
          });
        }
      }

      //const video = document.getElementById('cameraElement');
      const canvas = document.getElementById('cameraCanvas');
      if (net && context.isPosenetRunning) {
        net.estimateMultiplePoses(canvas, 0.5, false, 16, 5, 0.3, 30)
        .then(r => {
          console.log('People:', r.length, context.isConnectedToMarrow)
          if(r.length > 3 && context.isConnectedToMarrow && !context.waitingForStart) {
            context.setWaitingStatus(true);
            setTimeout(() => {
              console.log('Start now!');
              context.setWaitingStatus(false);
              context.setIsSliding(false)
              context.sendMarrowStart();
            }, 7000);
            setTimeout(() => {
              context.sendFrames();
              context.setShowPix2Pix(true);
            }, 37000);
          }
        })
      }


    }, 1000);
  }

  loadPoseNet = () => {
    posenet.load(1.01)
      .then(net => {
        this.setState({ net })
    })
  }

  render() {
    const { context } = this.props;
    const {  xTranslate, transitionTime } = this.state;
    const images = Array.apply(null, Array(context.amountOfImages)).map((x, i) => i);

    return (
      <div 
        ref='ImageSlider'
        className="ImageSlider" 
        style={{
          left: `${xTranslate}px`,
          transition: `left ${transitionTime}s cubic-bezier(0.21, 0.2, 0.49, 0.49) 0s, opacity 8s ease-in-out`,
          display: context.hide ? 'none' : 'inline'
      }}>
      <div 
        className="Images"
        style={{
          opacity: context.isSliding ? 1 : 0,
          transition: `all 7s ease-in-out`
        }}
      >
        {
          images.map(i => (
            <img
              id={`IMAGE_${i}`}
              ref={`IMAGE_${i}`}
              className="ImageInSlider"
              key={i}
              src={`${BASE_URL}/images/${i}.png`}
              alt="current" 
              width={context.imagesWidth}
              height={context.imagesHeight}
          />
          ))
        }
      </div>
      </div>
    );
  }
}

export default withContext(ImageSlider);