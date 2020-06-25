import React, { useEffect } from 'react';
import { useHistory, NavLink } from "react-router-dom";
import Title from './Title.js'

export default function Home() {
  let history = useHistory();

  useEffect(() => {
    const timeout = setTimeout(() => {
      history.push('/explore')
    }, 8000);
    return () => clearTimeout(timeout);
  }, []);

  return (
    <>
      <div className="inner">
        <Title
          lineContent1='GAN'
          lineContent2='LAT'
          lineContent3='ENT'
          lineContent4='SPA'
          lineContent5='CE'
          lineContent6='EXP'
          lineContent7='LOR'
          lineContent8='ER'
          lineContent9='a tool for exploring the latent space of Generative Adversarial Networks'
        />
      </div>
      <NavLink to="/explore" activeStyle={{ color: 'red' }}>EXPLORE TOOL</NavLink>
    </>
  );
}