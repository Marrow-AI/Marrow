import {createStore, compose, applyMiddleware } from 'redux';
const composeEnhancer = window.__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;

const animationStepHandler = (data) => {
  console.log("Received animation step!",data);
  store.dispatch(addAnimationStep(data.image));
}

const reducer = (state = {
  socket: null,
  snapshot: '',
  Get_Image: '',
  file_name: '',
  animationSteps: []
}, action) => {
  switch (action.type) {  
    case 'SET_SOCKET': {
      console.log("Setting socket", action.socket);
      if (state.socket) {
        state.socket.off('animationStep', animationStepHandler)
      }
      action.socket.on('animationStep', animationStepHandler)
      return {...state, socket: action.socket}
  }
  case 'SAVE_SNAPSHOT': {
    return {
      ...state,
      snapshot: action.snapshot
    }
  }
  case 'ADD_ANIMATION_STEP': {
    return {
      ...state,
      animationSteps: [...state.animationSteps,"data:image/jpeg;base64," + action.image]
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

export const addAnimationStep = (image) => ({
  type: 'ADD_ANIMATION_STEP',
  image
})

const store = createStore(
  reducer,
  composeEnhancer(applyMiddleware()),
);

export default store;


