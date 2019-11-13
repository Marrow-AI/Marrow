import React, { Component } from 'react';
import Credit from './Credit';
import logo from './img/marrow_logo.png';
import idfa from './img/idfa.png';

import runway from './img/runway_w.png';
import raycaster from './img/raycaster_w.png';
import iv from './img/iv_w.png';
import doclab from './img/doclab.png';
import nfb from './img/nfb.png';
import mit from './img/mit_w.png';
import './styles/App.css';

class App extends Component {
  render() {

    return (
      <div className="App">
        <div className="Background"></div>
        <div className="Opacity"></div>
        <div className="Header">
          <div className="Header-Left">
            <div className="Header-Left-Top">
              <img src={logo} alt="Marrow" className="Logo-Marrow"/>
              <ul>
                <li><a href="https://github.com/Raycasters/Marrow">GITHUB</a></li>
                <li><a href="https://immerse.news/when-machines-look-for-order-in-chaos-198fb222b60a">RESEARCH</a></li>
                <li><a href="mailto:shirinanlen@gmail.com">CONTACT</a></li>
              </ul>
            </div>
            <div className="Header-Left-Bottom">
              <p className="Header-Left-Description">an interactive experience about the mental states of the intelligent machines we create</p>
            </div>
          </div>
          <div className="Header-Right">
            <img src={idfa} alt="Idfa" className="Logo-Idfa"/>
          </div>
        </div>
        <div className="Main">
          <div className="Main-Title">
            <h2>I'VE</h2>
            <h2>ALWAYS</h2>
            <h2>BEEN</h2>
            <h2>JEALOUS</h2>
            <h2>OF OTHER</h2>
            <h2>PEOPLE'S</h2>
            <h2>FAMILIES</h2>
          </div>
          <div className="Main-Description">
            <h3>A MACHINE LEARNING INSTALLATION</h3>
            <h3>BY <a href="">SHIRIN ANLEN</a></h3>
            <h3>PROLOGUE TO MARROW</h3>
          </div>
        </div>
        <div className="Footer">
          <div className="Footer-Title">
            <h4>AN <a href="https://atlasv.io/" target='_blank' rel="noopener noreferrer">ATLAS V</a> AND <a href="http://raycaster.studio/" target='_blank' rel="noopener noreferrer">RAYCASTER</a> PRODUCTION IN CO-PRODUCTION WITH <a href="https://www.nfb.ca/" target='_blank' rel="noopener noreferrer">NATIONAL FILM BOARD OF CANADA</a> IN ASSOCIATION WITH <a href="https://runwayapp.ai/" target='_blank' rel="noopener noreferrer">RUNWAY</a> AND THE SUPPORT OF <a href="http://opendoclab.mit.edu/" target='_blank' rel="noopener noreferrer">MIT OPEN DOCUMENTARY LAB</a> & <a href="https://www.doclab.org/" target='_blank' rel="noopener noreferrer">IDFA DOCLAB</a></h4>
          </div>
          <div className="Footer-Credits">
            <Credit title='EXECUTIVE PRODUCER' name='EMMA DESSAU' link='http://www.emmadessau.info/'/>
            <Credit title='PRODUCE BY' name='ARNAUD COLINART' link='https://atlasv.io/'  large name2='~SHIRIN ANLEN' link2='https://shirin.works/'/>
            <Credit title='TECHNICAL DIRECTOR' name='CRISTOBAL VALENZUELA' link='https://cvalenzuelab.com/'/>
            <Credit title='3D DESIGNER & DEVELOPER' name='LAURA JUO-HSIN CHEN' link='http://www.jhclaura.com/'/>
            <Credit title='SPEECH DESIGNER' name='AVNER PELED' link='http://avner.js.org/'/>
            <Credit title='ADVISOR' name='ZIV SCHNEIDER' link='http://www.zivschneider.com/'/>
          </div>
          <div className="Footer-Logos">
            <a href="https://runwayapp.ai/" target='_blank' rel="noopener noreferrer"><img src={runway} alt="Runway AI"/></a>
             <a href="http://raycaster.studio/" target='_blank' rel="noopener noreferrer"><img src={raycaster} alt="raycaster" id='Raycaster-Logo'/></a>
             <a href="https://atlasv.io/" target='_blank' rel="noopener noreferrer"><img src={iv} alt="iv"/></a>
             <a href="https://www.nfb.ca/" target='_blank' rel="noopener noreferrer"><img src={nfb} alt="nfb"/></a>
             <a href="https://www.doclab.org/" target='_blank' rel="noopener noreferrer"><img src={doclab} alt="co creation"/></a>
             <a href="http://opendoclab.mit.edu/" target='_blank' rel="noopener noreferrer"><img src={mit} alt="co creation" id='MIT-Logo'/></a>
          </div>
        </div>
      </div>
    );
  }
}

export default App;
