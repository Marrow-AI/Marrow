import React, { Component } from 'react';
import ReactDOM from 'react-dom';
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
      transitionTime: 0
    }
  }

  componentDidMount(){
    const { context } = this.props;
    this.setState({
      xTranslate: 0,
      transitionTime: TIME
    });

    setInterval(() => {
      const { context } = this.props;
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
          transitionTime: 2
        });
      } else {
        if (context.centerImage === 0) {
          this.setState({ 
            xTranslate: -context.amountOfImages*WIDTH,
            transitionTime: 0
          });
        } else {
          this.setState({ 
            xTranslate: 0,
            transitionTime: TIME
          });
        }
      }
    }, 1000);
  }

  render() {
    const { context } = this.props;
    const { leftImage, xTranslate, transitionTime } = this.state;
    const images = Array.apply(null, Array(context.amountOfImages)).map((x, i) => i);
    
    return (
      <div 
        ref='ImageSlider'
        className="ImageSlider" 
        style={{
          left: `${xTranslate}px`,
          transition: `all ${transitionTime}s cubic-bezier(0.21, 0.2, 0.49, 0.49) 0s`
      }}>
      <div 
        className="Images"
        style={{
          opacity: context.isSliding ? 1 : 0,
          transition: `all 7s`
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