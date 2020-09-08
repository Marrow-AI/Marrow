import React from 'react';
import { NavLink } from "react-router-dom";

export default function About() {

  return (
    <>
      <div className="aboutSection">
        <NavLink className="exit" to="/explore" activeStyle={{ color: 'red' }}> X </NavLink>
        <div className="aboutDiv">
          <h1 className="aboutTitle">ABOUT</h1>
          <p className="aboutText"> How to use:</p>
        </div>
      </div>
    </>
  )
}