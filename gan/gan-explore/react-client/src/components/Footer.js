import React from 'react';
import { NavLink } from 'react-router-dom';

const Footer = () => {
  return (
    <div className="footerCointainer">
    <div className="footerDiv">
          <div>
            <NavLink className='footer' to="/" exact activeClassName="active">Start Again</NavLink>
            <a className='footer' href="https://shirin.works/Marrow-teach-me-how-to-see-you-mother-Machine-learning-immersive" alt="" target="_blank" rel="noopener noreferrer">About Marrow</a>
            <a className='footer' href="https://towardsdatascience.com/a-tool-for-collaborating-over-gans-latent-space-b7ea92ad63d8" alt="" target="_blank" rel="noopener noreferrer">About the tool</a>
          </div>
          <div >
            <div className='logosdiv'>
            <a href='https://atlasv.io/' target="_blank"><img className='logos' src="/static/atlasV.png" alt='' /></a>
            <a href='https://www.nfb.ca/interactive/marrow' target="_blank"><img className='logos' src="/static/NFB.png" alt='' /></a>
            <a href='https://ars.electronica.art/news/de/' target="_blank"><img className='logos' src="/static/Ars-Electronica.png" alt='' /></a>
            </div>
          </div>
        </div>
    </div>
  );
};

export default Footer;
