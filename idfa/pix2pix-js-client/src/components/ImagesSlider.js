import React, { Component } from 'react';
import { withContext } from './Provider';

import '../styles/ImageSlider.css';

const BASE_URL = 'http://localhost:3000'

class ImageSlider extends Component {
  render() {
    const { context } = this.props;
    const images = Array.apply(null, Array(context.amountOfImages)).map((x, i) => i);
    console.log(Date.now())
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
          />
          ))
        }
      </div>
      </div>
    );
  }
}

export default withContext(ImageSlider);