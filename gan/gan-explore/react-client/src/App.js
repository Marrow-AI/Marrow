import React from 'react';
import { Route, BrowserRouter as Router } from 'react-router-dom'
import './App.scss';
import Generate from './components/Generate.js';
import Home from './components/Home.js';
import About from './components/About.js';
import socketIOClient from "socket.io-client";
import store, { setSocket } from './state'

const ENDPOINT = "http://localhost:8540";

const socket = socketIOClient(ENDPOINT);

console.log("Connecting to socket");
socket.on('connect', () => {
  console.log("Socket connected!", socket.id);
  store.dispatch(setSocket(socket));
});


function App() {
  return (
    <>
    <div className='app-container'>
      <Router>
        <Route exact path="/" component={Home} />
        <Route exact path="/explore" component={Generate} />
        <Route exact path="/about" component={About} />
      </Router>
    </div>
    </>
  );
}

export default App;
