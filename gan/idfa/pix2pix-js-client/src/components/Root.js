import React, { Component } from 'react';
import { AppProvider } from './Provider';
import App from './App';

class Root extends Component {
  render() {
    return (
      <AppProvider> 
        <App />
      </AppProvider>
    );
  }
}

export default Root;
