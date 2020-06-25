import React from 'react';
import { Route } from "react-router-dom";
import './App.scss';
import Generate from './components/Generate.js';
import Home from './components/Home.js';
import About from './components/About.js';

const routes = [
  { path: "/", name: "Home", Component: Home },
  { path: "/explore", name: "Generate", Component: Generate },
  { path: "/about", name: "About", Component: About }
]

function App() {
  return (
    <>
      {routes.map(({ path, Component }) => (
        <Route key={path} path={path} exact>
          <Component />
        </Route>
      ))}
    </>
  );
}

export default App;
