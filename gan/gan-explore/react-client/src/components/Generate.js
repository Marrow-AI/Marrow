import React, { useState } from "react";
import { useSelector } from 'react-redux';
import { useForm } from "react-hook-form";
import snapshots from './snapshots.json';
import Footer from './Footer.js';
import { makeStyles } from '@material-ui/core/styles';
import InputLabel from '@material-ui/core/InputLabel';
import MenuItem from '@material-ui/core/MenuItem';
import FormHelperText from '@material-ui/core/FormHelperText';
import FormControl from '@material-ui/core/FormControl';
import Select from '@material-ui/core/Select';
import store, {clearAnimationSteps, moveSteps} from '../state';
import SaveForm from "./SaveForm";
import EncoderSection from "./EncoderSection";

const useStyles = makeStyles((theme) => ({
  root: {
    background: "black",
    border: "white",
    backgroundColor: "black"
  },
  formControl: {
    margin: theme.spacing(1),
    minWidth: 120,
  },
  selectEmpty: {
    marginTop: theme.spacing(2),
  }
}))

//const ENDPOINT = 'http://52.206.213.41:8080';
const ENDPOINT = '';

export default function Generate() {
  const [view, setView] = useState();
  const { register, handleSubmit } = useForm({ mode: "onBlur" });
  const classes = useStyles();
  const [dataset, setDataset] = useState('');
  const [snapshot, setSnapshot] = useState('ffhq');
  const [generating, setGenerating] = useState('both');
  const [steps, setSteps] = useState(144);
  const animationSteps = useSelector(state => state.animationSteps);
  const currentStep = useSelector(state => state.currentStep);

  const handleChange = (event) => {
    setDataset(event.target.value);
  };

  const handleSnapshot = (event) => {
    setSnapshot(event.target.value);
    store.dispatch({
      type: 'SAVE_SNAPSHOT',
      snapshot: snapshot
    })
  }

  const handleGenerating = (event) => {
    setGenerating(event.target.value);
  }

  const onSubmit = (values, ev) => {
    const form = ev.target;
    const data = {
      steps: form.steps.value,
      snapshot: form.snapshot.value,
      type: form.shuffle.value,
      currentStep: currentStep
    }
    store.dispatch(clearAnimationSteps());
    fetch(ENDPOINT + '/shuffle', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    })
      .then(res => res.json())
      .then((data) => {
        if (data.result === "OK") {
          return fetch(ENDPOINT + '/publish', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
          })
          //return getImage("forward", "1");
        } else {
          alert(data.result);
        }
      })
      .then(res => res.json())
      .then((data) => {
        console.log("Publish result", data);
        if (data.result === "OK") {
            console.log("Server is publishing!");
        } else {
          alert(data.result);
        }
      })
  };

  const getImage = async (direction, steps) => {
    await fetch(ENDPOINT + '/generate?direction=' + direction + '&steps=' + steps + '&shadows=0')
      .then(response => response.json())
      .then(data => {
        setView("data:image/jpeg;base64," + data.result)
      }).catch(err => {
        console.log("Error Reading data " + err);
      })
  }

  const handleDirection = (e) => {
    e.preventDefault()
      /*
    getImage(
      e.currentTarget.dataset.direction,
      e.currentTarget.dataset.steps
    )*/
    store.dispatch(moveSteps(
      e.currentTarget.dataset.direction,
      e.currentTarget.dataset.steps
    ))
  }

  return (
    <>
      <h1 className="secondTitle">EXPLORER TOOL</h1>
      <div className="main">
        <div className="mainSection" >
          <form className="formLeft" key={1} className="shuffleForm" onSubmit={handleSubmit(onSubmit)} >
            <FormControl className={classes.formControl} >
              <InputLabel className="inputNew" id="demo-simple-select-helper-label">Choose a dataset</InputLabel>
              <Select className="select dataset" name="type" autoComplete="off"
                labelId="demo-simple-select-helper-label"
                id="demo-simple-select-helper"
                value={dataset}
                onChange={handleChange}
                ref={register}
              >
                <MenuItem value={"person"}>This Person Does Not Exist</MenuItem>
                <MenuItem value={"happy"} >Happy Families Dinner</MenuItem>
              </Select>
              <FormHelperText>Load a dataset of your intreset</FormHelperText>
            </FormControl>
      
            <FormControl className={classes.formControl}>
              <InputLabel className="inputNew" id="demo-simple-select-helper-label" >Choose a Snapshot</InputLabel>
              <Select className="select snapshot" name="snapshot" autoComplete="off"
                labelId="demo-simple-select-helper-label"
                id="demo-simple-select-helper"
                value={snapshot}
                onChange={handleSnapshot}
                ref={register}
              >
                {snapshots.snapshots.snapFamily.map(value => (
                  <MenuItem className="snapshot" value={value} key={value} >{value}</MenuItem>
                ))}
              </Select>
              <FormHelperText>Load a number of Snapshot from the choosen Dataset</FormHelperText>
            </FormControl>

            <FormControl className={classes.formControl}>
              <InputLabel className="inputNew" id="demo-simple-select-helper-label">Generating Options</InputLabel>
              <Select className="select shuffle" name="shuffle" autoComplete="off"
                labelId="demo-simple-select-helper-label"
                id="demo-simple-select-helper"
                value={generating}
                onChange={handleGenerating}
                ref={register}
              >
                <MenuItem value={"both"} >Shuffle both source and destination</MenuItem>
                <MenuItem value={"keep_source"} >Keep source and shuffle destination</MenuItem>
                <MenuItem value={"use_dest"} >Use destination as the next source</MenuItem>
                <MenuItem value={"use_step"} >Use current step as source</MenuItem>
              </Select>
              <FormHelperText>Choose how to generate your animations</FormHelperText>
            </FormControl>

            <div className="stepsDiv">
              <label className="label steps"> Number of steps:</label>
              <input className="input steps" autoComplete="off" name="steps" defaultValue={steps} type="number"/>
            </div>

            <div className="divBtnGnr">
              <button className="btn generate" name="generate" type="onSubmit" ref={register}>Generate animation</button>
            </div>
          </form>
          
          <div className="imgControler">
            <div className="output-container">
            <img className="imgAnimation" src={animationSteps.length > 0 ? animationSteps[currentStep] : ''} width="512" height="512" alt="" />
            </div>
            <div className="controls-container">
              <button onClick={handleDirection} className="direction" data-direction="back" data-steps="1">&lt;</button>1
              <button onClick={handleDirection} className="direction" data-direction="forward" data-steps="1">&gt;</button>
              <button onClick={handleDirection} className="direction" data-direction="back" data-steps="10">&lt;&lt;</button>10
              <button onClick={handleDirection} className="direction" data-direction="forward" data-steps="10">&gt;&gt;</button>
              <button onClick={handleDirection} className="direction" data-direction="back" data-steps="100">&lt;&lt;&lt;</button>100
              <button onClick={handleDirection} className="direction" data-direction="forward" data-steps="100">&gt;&gt;&gt;</button>
            </div>
            <SaveForm />
           </div>
         <EncoderSection />
        </div>
       </div>
     <Footer />
    </>
  );
}
