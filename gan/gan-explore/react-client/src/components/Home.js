import React from 'react';
import {NavLink} from "react-router-dom";
import Title from './Title.js'
export default function Home() { 

  return (
    <>
      <Title 
          lineContent1 ='GAN' 
          lineContent2 ='LAT'
          lineContent3 ='ENT'
          lineContent4 ='SPA'
          lineContent5 ='CE'
          lineContent6 ='EXP'
          lineContent7 ='LOR'
          lineContent8 ='ER'/>
      
      <NavLink to="/explore" activeStyle={{color:'red'}}>EXPLORE TOOL</NavLink>

   </>
  );
}