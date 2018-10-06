import React, { Component } from 'react';
import logo from './img/logo.svg';
import logoHover from './img/logo_hover.svg';
import runway from './img/runway.png';
import raycaster from './img/raycaster.png';
import iv from './img/iv.png';
import cocreation from './img/cocreaction.png';
import mit from './img/mit.png';
import './styles/App.css';

class App extends Component {
  state = {
    isHovering: false
  }

  render() {
    const { isHovering } = this.state;

    return (
      <div className="App">
        <div className="Header">
          <h2>COMING SOON</h2>
          <img 
            src={isHovering ? logoHover : logo}
            className="Logo"
            onMouseOver={() => this.setState({ isHovering: true })}
            onMouseLeave={() => this.setState({ isHovering: false })} 
          />
          <h3>A tale of nonsense in the age of intelligence played by machine learning actors</h3>
        </div>
        <div className="Links">
          <ul>
            <li>
              <a href="https://github.com/Raycasters/Marrow">GITHUB</a>
            </li>
            <li>
              <a href="http://process.marrow.raycaster.studio/">PRODUCTION BLOG</a>
            </li>
            <li>
              <a href="https://becominghuman.ai/5-reasons-why-i-believe-ai-can-have-mental-illness-b289e1601eb2">WHY</a>
            </li>
            <li>
              <a href="mailto:shirinanlen@gmail.com">CONTACT</a>
            </li>
          </ul>
        </div>

        <div className="Credits">
          <h4>MARROW is an interactive experience about the mental states of the intelligent machines we create</h4>
          <h4>by <a href="https://shirin.works/">~shirin anlen</a></h4>
          <h5>technical director: <a href="https://cvalenzuelab.com/">Cristóbal Valenzuela</a></h5>
          <h5>experience designer: <a href="http://zivschneider.com/">Ziv Schneider</a></h5>
          <h5>co-writer: <a href="http://eranhadas.com/english/">Eran Hadas</a></h5>
          <h5>first Character artist: <a href="http://www.jhclaura.com/">Laura Jou-Hsin Chen</a></h5>
          <h5>executive producer: <a href="http://www.emmadessau.info/">Emma Dessau</a></h5>
          <h5>producer: <a href="https://atlasv.io/">Arnaud Colinart</a></h5>
        </div>      

        <div className='Logos'>
          <a href="https://runwayml.com/"><img src={runway} alt="Runway"/></a> 
          <a href="http://raycaster.studio/"><img src={raycaster} alt="raycaster" className="LargeLogo"/></a> 
          <a href="https://atlasv.io/"><img src={iv} alt="IV"/></a>
          <img src={cocreation} alt="IV"/>
          <a href="http://opendoclab.mit.edu/"><img src={mit} alt="Mit doc lab" className="LargeLogo"/></a> 
        </div>
      </div>
    );
  }
}

export default App;
