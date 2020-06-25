import React, { useEffect } from 'react';
import Title from './Title.js';
import { gsap } from 'gsap';

export default function Logo() {

  useEffect(() => {
    gsap.fromTo("logoPage", { opacity: 0, ease: "power3.out", },
      {
        opacity: 1,
        delay: 2.2,
        duration: 2
      });
  }, [])

  return (
    <>
      <div className="logoPage">
        <Title
          lineContent2='LAT'
          lineContent3='ENT'
          lineContent4='SPA'
          lineContent5='CE'
          lineContent6='EXP'
          lineContent7='LOR'
          lineContent8='ER' />
      </div>
    </>
  );
}