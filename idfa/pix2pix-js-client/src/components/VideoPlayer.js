import React, {Component} from 'react';
import { AppContext } from './Provider';
import '../styles/VideoPlayer.css';

class Camera extends Component {
  render() {
    return (
      <div className="VideoPlayer">
        <video 
          style={{opacity: this.props.context.opacities[this.props.type]}}
          autoPlay="true" 
          id={this.props.type + 'Element'}/>
      </div>
    );
  }
}

export default props => (
  <AppContext.Consumer>
    {context => <Camera {...props} context={context} />}
  </AppContext.Consumer>
);