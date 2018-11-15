import React, { Component } from 'react';
import { withContext } from './Provider';

import '../styles/CenterImage.css';

const BASE_URL = 'http://localhost:3000';

class CenterImage extends Component {
  render() {
    const { context } = this.props;

    return (
      <div
        style={{
          opacity: context.hide ? 0 : 1 // 0 : 1 
        }}
        >
        <img 
          className="CenterImage"
          id="CenterImage"
          alt='center'
          //src={`images/${context.centerImage}.png`}
          src={`${BASE_URL}/images/${context.centerImage}.png`}
          style={{
            opacity: context.isSliding ? 0 : 1, // 0 : 1
            transition: context.isSliding ? 'opacity 0s' : 'opacity 25s'  // 0 : 1
          }}
          width={context.imagesWidth}
          height={context.imagesHeight}
        />
      </div>
    );
  }
}

export default withContext(CenterImage);