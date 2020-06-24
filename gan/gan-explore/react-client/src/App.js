import React from 'react';
import { BrowserRouter as Router, Switch, Route, Link } from "react-router-dom";
import { useHistory } from "react-router";
import './App.css';
import Generate from './components/Generate.js';
import Home from './components/Home.js';
import About from './components/About.js';


function App() {

  return (
    <div className="App">
      <Router>
        <div>
          <Switch>
            <Route exact path="/" component={Home} />
            <Route path="/explore" component={Generate} />
            <Route path='/about' component={About} />
          </Switch>
        </div>
      </Router>
    </div>
  );
}

export default App;
