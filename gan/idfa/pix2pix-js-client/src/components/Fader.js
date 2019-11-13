import React, { Component } from 'react';
import { withContext } from './Provider';

import '../styles/Fader.css';

class Fader extends Component {
  render() {
    return (
      <div className="Fader" style={{
        opacity: this.props.context.faderOpacity
      }}/>
    );
  }
}

export default withContext(Fader);