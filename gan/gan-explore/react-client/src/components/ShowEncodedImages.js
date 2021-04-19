import React, { useState } from 'react';
import store from '../state';
import Tree from 'react-tree-graph';
import 'react-tree-graph/dist/style.css';
import TreeChart from './TreeChart';
import { useSelector } from 'react-redux';

const initialData = {
  name: "John",
  parent_id: "123",
  image: "https://freight.cargo.site/w/1500/q/75/i/86ea540d3a129ee6f716806b5c2eec2de144339a1abbd00297dfdaddead631e9/posterHorizontal.jpg",
    children: [{
          name: "Bob",
        },
        {
          name: "Paul",
          parent_id: "687",
          image: "https://freight.cargo.site/w/1500/q/75/i/86ea540d3a129ee6f716806b5c2eec2de144339a1abbd00297dfdaddead631e9/posterHorizontal.jpg",
          children: [{
                name: "Mary",
          },{
                name: "Jane",
                image: "https://freight.cargo.site/w/1500/q/75/i/86ea540d3a129ee6f716806b5c2eec2de144339a1abbd00297dfdaddead631e9/posterHorizontal.jpg",
                children: [{
                  name: "Bill",
                },
                {
                  name: "August",
                  image: "https://freight.cargo.site/w/1500/q/75/i/86ea540d3a129ee6f716806b5c2eec2de144339a1abbd00297dfdaddead631e9/posterHorizontal.jpg"
                }, 
                {
                  name: "Yoyo",
                  image: "http://lorempixel.com/60/60/cats/5"
                }]
              }, {
                name: "Nathan",
              },{
                name: "Ringwald",
                image: "http://lorempixel.com/60/60/cats/6"
              }]
    }]
};

export default function ShowEncodedImages(props) {

  const [data, setData] = useState(initialData);
  

  return (
    <div className='tree'>
      <TreeChart data={data} />
      <button onClick={() => setData(initialData.children[0])}>
        Update data
      </button>
      {/* <Tree
        data={data}
        height={400}
        width={400} 
        /> */}

    </div>
  )
}
