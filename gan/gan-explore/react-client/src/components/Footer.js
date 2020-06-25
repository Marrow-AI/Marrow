import React from 'react';
import { Route, Link } from "react-router-dom";
import About from './About.js';

export default function Footer() { 

  return(
      <div className="footer">
        <Link to="/about">ABOUT</Link>
        <Link to={{ pathname: "https://shirin.works/" }} target="_blank" > DATASET TOOL </Link> 
    
       <Route path='/about' component={About} />
      </div>
  )
}