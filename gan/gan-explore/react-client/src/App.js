import React from 'react';
import { Route, Link } from "react-router-dom";
import './App.scss';
import Generate from './components/Generate.js';
import Home from './components/Home.js';
import About from './components/About.js';
import {CSSTransition} from 'react-transition-group';
import {gsap} from 'gsap';


const routes = [
  {path: "/", name: "Home", Component: Home},
  {path: "/explore", name: "Generate", Component: Generate},
  {path: "/about", name: "About", Component: About}
]

function App() {

  return (
   <>
    <div className="container">
      {routes.map(({ path, Component }) => (
        <Route key="name" path={path} exact>
         
            <div className="page">
              <Component />
            </div>
           
        </Route>
      ))}
     </div>
    </>
  );
}

export default App;
