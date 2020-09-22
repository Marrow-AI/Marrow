import {createStore, compose, applyMiddleware } from 'redux';
const composeEnhancer = window.__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;

const reducer = (state = {
  socket: null,
  snapshot: '',
  Get_Image: '',
  file_name: '',
}, action) => {
  switch (action.type) {  
    case 'SET_SOCKET': {
      console.log("Setting socket", action.socket);
      return {...state, socket: action.socket}
  }
  case 'SAVE_SNAPSHOT': {
    return {
      ...state,
      snapshot: action.snapshot
    }
  }
  case 'GET_IMAGE': {
    return {
      ...state,
      Get_Image: action.getImage
    }
  }
    case 'SAVE_FILE_NAME': {
      return {
        ...state,
        file_name: action.images
    }
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


