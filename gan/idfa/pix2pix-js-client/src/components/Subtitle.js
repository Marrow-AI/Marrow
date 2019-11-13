import React, { Component } from 'react';
import { withContext } from './Provider';

import '../styles/Subtitle.css';

class Subtitle extends Component {
  render() {
    return (
      <div className="Subtitle">
        <p className="Subtitle--First-Row">[softly] If we turn our backs, we haven't got a chance.</p>
        <p className="Subtitle--Second-Row"></p>
      </div>
    );
  }
}

export default withContext(Subtitle);