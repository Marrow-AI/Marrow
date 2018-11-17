import React, { Component } from 'react';
import ReactDOM from 'react-dom';
import { withContext } from './Provider';
import '../styles/ImageSlider.css';


const BASE_URL = 'http://localhost:3000'
const WIDTH = 218;
const TIME = 1200;

class ImageSlider extends Component {
  constructor(props){
    super(props);
    this.state = {
      imgInCenter: this.props.context.amountOfImages - 1,
      xTranslate: -this.props.context.amountOfImages*WIDTH+(window.innerWidth*0.6),
      transitionTime: 0,
      net: null
    }
  }
 
  componentDidMount(){
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
          if (right > window.innerWidth/2 - 100 && right < window.innerWidth/2 + 100) {
            if(context.isSliding) {
              console.log('center image', i)
              context.setCenterImage(i);
            }
          }
        }
      }

      
      if (context.resetSlider) {
        this.setState({ 
          xTranslate: -this.props.context.amountOfImages*WIDTH+(window.innerWidth*0.6),
          transitionTime: 0
        });
        context.setCenterImage(context.amountOfImages - 1);
        context.setResetSlider(false);
      }

      if(!context.isSliding) {
        const center = ReactDOM.findDOMNode(this.refs[`IMAGE_${context.centerImage}`]).getBoundingClientRect();
        let deltaAlignToCenter = window.innerWidth/2 - center.left - 100;
        let transitionTime = 1;
        if (center.left > window.innerWidth/2 + 100) {
          deltaAlignToCenter = deltaAlignToCenter * -1;
        } 

        if (deltaAlignToCenter > 70 && deltaAlignToCenter < 80) {
          transitionTime = 2;
        } else if (deltaAlignToCenter > 80) {
          transitionTime = 3;
        }
        this.setState({ 
          xTranslate: pos.left + deltaAlignToCenter,
          transitionTime
        });

      } else {
        if (context.centerImage === 20) {
          this.setState({ 
            xTranslate: -this.props.context.amountOfImages*WIDTH+(window.innerWidth*0.6),
            transitionTime: 0
          });
          context.setCenterImage(context.amountOfImages - 1);
        } else {
          this.setState({ 
            xTranslate: window.innerWidth,
            transitionTime: TIME
          });
        }
      }

    }, 1000);
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
          transition: `left ${transitionTime}s cubic-bezier(0.21, 0.2, 0.49, 0.49) 0s, opacity 15s ease-in-out`,
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
              //src={`images/${i}.png`}
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
