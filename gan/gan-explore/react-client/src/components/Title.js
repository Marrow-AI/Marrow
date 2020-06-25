import React, { useEffect, useRef } from 'react';
import { gsap } from 'gsap';

export default function Title({
  lineContent1, lineContent2, lineContent3, lineContent4, lineContent5, lineContent6, lineContent7, lineContent8
}) {
  let line1 = useRef(null);
  let line2 = useRef(null);
  let line3 = useRef(null);
  let line4 = useRef(null);
  let line5 = useRef(null);
  let line6 = useRef(null);
  let line7 = useRef(null);
  let line8 = useRef(null);

  useEffect(() => {
    gsap.from([ line2, line3, line4, line5, line6, line7, line8], 0.8, {
      delay:.8,
      ease: "power3.out",
      y: 64,
      stagger: {
        amount: 0.1
      }
    })

  }, [line1, line2, line3, line4, line5, line6, line7, line8])

  return (
    <h1 className="page-title">
      <div className="line-wrap big">
        <div ref={el => (line1 = el)} className="lineBig">
          {lineContent1}
        </div>
      </div>
      <div className="line-wrap">
        <div ref={el => (line2 = el)} className="line">
          {lineContent2}
        </div>
      </div>
      <div className="line-wrap">
        <div ref={el => (line3 = el)} className="line">
          {lineContent3}
        </div>
      </div>
      <div className="line-wrap">
        <div ref={el => (line4 = el)} className="line">
          {lineContent4}
        </div>
      </div>
      <div className="line-wrap">
        <div ref={el => (line5 = el)} className="line">
          {lineContent5}
        </div>
      </div>
      <div className="line-wrap">
        <div ref={el => (line6 = el)} className="line">
          {lineContent6}
        </div>
      </div>
      <div className="line-wrap">
        <div ref={el => (line7 = el)} className="line">
          {lineContent7}
        </div>
      </div>
      <div className="line-wrap">
        <div ref={el => (line8 = el)} className="line">
          {lineContent8}
        </div>
      </div>
    </h1>
  );
}