import {createStore, compose, applyMiddleware } from 'redux';
const composeEnhancer = window.__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;

const animationStepHandler = (data) => {
  store.dispatch(addAnimationStep(data.image, data.step));
}

const reducer = (state = {
  socket: null,
  snapshot: '',
  Get_Image: '',
  file_name: '',
  animationSteps: [],
  currentStep: 0,
  maxSteps: 144 
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
      animationSteps: [...state.animationSteps,"data:image/jpeg;base64," + action.image],
      currentStep: action.step
    }
  }
  case 'CLEAR_ANIMATION_STEPS': {
    return {
      ...state,
      animationSteps: [],
      currentStep: 0
    }
  }
  case 'MOVE_STEPS': {
    const steps = action.direction == 'back' ? action.steps * -1 : Number(action.steps)
    return {
      ...state,
      currentStep: Math.min(Math.max(state.currentStep + steps, 0),state.animationSteps.length - 1)
    }
  }
  case 'SET_STEP': {
    return {
      ...state,
      currentStep: action.step
    }
  }
  case 'SET_MAX_STEPS': {
    return {
      ...state,
      maxSteps: action.maxSteps
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

export const addAnimationStep = (image, step) => ({
  type: 'ADD_ANIMATION_STEP',
  image,
  step
})

export const clearAnimationSteps  = () => ({
  type: 'CLEAR_ANIMATION_STEPS'
})

export const moveSteps  = (direction, steps) => ({
  type: 'MOVE_STEPS',
  direction,
  steps
})

export const setStep  = (step) => ({
  type: 'SET_STEP',
  step
})

export const setMaxSteps  = (maxSteps) => ({
  type: 'SET_MAX_STEPS',
  maxSteps
})

const store = createStore(
  reducer,
  composeEnhancer(applyMiddleware()),
);

export default store;


