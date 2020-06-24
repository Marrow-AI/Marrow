import React from 'react';
import { useHistory } from "react-router-dom";
import { BrowserRouter as Router, Switch, Route, Link } from "react-router-dom";


export default function Home() { 
  let history = useHistory();


  function handleClick() {
    history.push("/explore");
  }

  return (
    <div>
      <h1 className="homeLogo">
        GAN<br/>
        LAT<br/>
        ENT<br/>
        SPA<br/>
        CE<br/>
        EXP<br/>
        LOR<br/>
        ER</h1>
    

      <button type="button" onClick={handleClick}>Explore</button>
   </div>
    );
}