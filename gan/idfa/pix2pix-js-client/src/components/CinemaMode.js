import React, { Component } from 'react';
import { withContext } from './Provider';

import '../styles/CinemaMode.css';

class CinemaMode extends Component {
  render() {
    const { cinemaModeSize } = this.props.context;
    return (
      <div className="CinemaMode">
        <div className="CinemaMode--Top" style={{ height: cinemaModeSize }} />
        <div className="CinemaMode--Bottom" style={{ height: cinemaModeSize }} />
      </div>
    );
  }
}

export default withContext(CinemaMode);