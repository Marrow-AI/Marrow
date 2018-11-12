import React, { Component } from 'react';
import { withContext } from './Provider';

import '../styles/CenterImage.css';

const BASE_URL = 'http://localhost:3000'

class CenterImage extends Component {
  render() {
    const { context } = this.props;

    return (
      <img 
        className="CenterImage"
        id="CenterImage"
        src={`${BASE_URL}/images/${context.centerImage}.png`}
        style={{
          opacity: context.isSliding ? 0 : 1
        }}
      />
    );
  }
}

export default withContext(CenterImage);