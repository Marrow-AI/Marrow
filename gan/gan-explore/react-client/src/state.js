import {createStore, compose, applyMiddleware } from 'redux';
const composeEnhancer = window.__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;


const reducer = (state = {
  socket: null
}, action) => {
  switch (action.type) {  
    case 'SET_SOCKET': {
      console.log("Setting socket", action.socket);
      return {...state, socket: action.socket}
  }
  default:
      return state;
  }
}

export const setSocket = (socket) => ({
  type: 'SET_SOCKET',
  socket
})

const store = createStore(
  reducer,
  composeEnhancer(applyMiddleware()),
);

export default store;


