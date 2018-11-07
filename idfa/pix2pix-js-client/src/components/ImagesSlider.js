import React, { Component } from 'react';
import { withContext } from './Provider';

import '../styles/ImageSlider.css';

const BASE_URL = 'http://localhost:3000'

class ImageSlider extends Component {
  constructor(){
    super();
    this.state = {
      leftImage: 0,
      xTranslate: 0,
    }
  }

  componentDidMount(){
    const { context } = this.props;
    setInterval(() => {
      const { leftImage } = this.state;
      const { context } = this.props;
      let update;
      let xTranslate = 240*leftImage;
      if (leftImage < context.amountOfImages) {
        update = leftImage + 1;
        xTranslate = xTranslate*-1;
      } else {
        update = 0;
        xTranslate = 0;
      }

      this.setState({ 
        leftImage: update,
        xTranslate
      })
    }, context.sliderSpeed*1000);
  }

  render() {
    const { context } = this.props;
    const { leftImage, xTranslate } = this.state;
    const images = Array.apply(null, Array(context.amountOfImages)).map((x, i) => i);
    console.log(leftImage);
    return (
      <div 
        className="ImageSlider" 
        style={{
        opacity: context.imageSliderOpacity
      }}>
      <div className="Images">
        {
          images.map(i => (
            <img
              className="ImageInSlider"
              key={i}
              src={`${BASE_URL}/images/${i}.png`}
              alt="current" 
              width={context.imagesWidth}
              height={context.imagesHeight}
              style={{
                transform: `translate(${xTranslate}px, 0px)`,
                transition: `all ${context.sliderSpeed+0.2}s linear`
              }}
          />
          ))
        }
      </div>
      </div>
    );
  }
}

export default withContext(ImageSlider);