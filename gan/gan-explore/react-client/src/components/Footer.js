import React from 'react';
import { NavLink } from 'react-router-dom';

const Footer = () => {
  return (
    <div className="footerCointainer">
    <div className="mainFooter">
      <div className="footer">
        <NavLink className='footer' to="/about" exact activeClassName="active">About the tool</NavLink>
        <a className='footer' href="https://www.nfb.ca/interactive/marrow/" alt="" target="_blank" rel="noopener noreferrer">About Marrow</a>
        <a className='footer' href="https://towardsdatascience.com/small-family-small-dataset-7f7db708f06d" alt="" target="_blank" rel="noopener noreferrer">Tool's Background</a>
        <a className='footer' href="/" alt="" target="_blank" rel="noopener noreferrer">Section for Logos</a>
      </div>
    </div>
    </div>
  );
};

export default Footer;
