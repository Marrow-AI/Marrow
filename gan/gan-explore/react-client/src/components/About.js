import React from 'react';
import { NavLink } from "react-router-dom";

export default function About() {
 
  return (
    <>
    <NavLink to="/explore" activeStyle={{color:'red'}}> X </NavLink>

    <div className="aboutSection">
      <div className="aboutDiv">

        <h4 className="aboutTitle">ABOUT</h4>
        <h2 className="about">Et tortor at risus viverra adipiscing at in. Ultricies integer quis auctor elit. Viverra suspendisse potenti nullam ac tortor vitae purus faucibus. Risus nec feugiat in fermentum posuere urna nec tincidunt praesent. Malesuada pellentesque elit eget gravida cum sociis natoque penatibus. Tristique senectus et netus et malesuada. Elementum eu facilisis sed odio.
        Ornare <span className="hl">suspendisse sed nisi lacus. Eu turpis</span> egestas pretium aenean pharetra magna ac placerat. Non enim praesent elementum facilisis leo vel fringilla. Eget lorem dolor sed viverra ipsum nunc aliquet. Sit amet mattis vulputate enim nulla aliquet porttitor lacus. Sit amet nisl suscipit adipiscing bibendum est ultricies integer. Sed vulputate odio ut enim blandit volutpat maecenas.
        In iaculis nunc sed augue lacus. <span className="hl">Tristique senectus et netus et.</span> Consequat nisl vel pretium lectus quam. Nulla pharetra diam sit amet nisl suscipit adipiscing bibendum est. Faucibus vitae aliquet nec ullamcorper sit. Rhoncus est pellentesque elit ullamcorper dignissim cras tincidunt lobortis feugiat. Turpis cursus in hac habitasse platea dictumst. Commodo odio aenean sed adipiscing.
        Bibendum est ultricies integer quis auctor elit sed.
      </h2>
      </div>
    </div>
    </>
  )
}