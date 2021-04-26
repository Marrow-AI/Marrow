import React, { useState, useEffect } from "react";
import { useForm } from "react-hook-form";
import { makeStyles } from '@material-ui/core/styles';
import InputLabel from '@material-ui/core/InputLabel';
import MenuItem from '@material-ui/core/MenuItem';
import FormHelperText from '@material-ui/core/FormHelperText';
import FormControl from '@material-ui/core/FormControl';
import Select from '@material-ui/core/Select';
import { useSelector } from 'react-redux';
import store, {clearAnimationSteps, setMaxSteps} from '../state';

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

export default function SaveForm() {
  const [animation, setAnimation] = useState([]);
  const { register: register2, handleSubmit: handleSubmit2 } = useForm({ mode: "onBlur" });
  const { register: register3, handleSubmit: handleSubmit3 } = useForm({ mode: "onBlur" });
  const classes = useStyles();
  const [animationClip, setanimationlip] = useState('');
  const snapshots = useSelector(state => state.snapshot);
  const getImage = useSelector(state => state.getImage);
  const ENDPOINT = useSelector(state => state.ENDPOINT);

  const handleSave = (values, e) => {
    e.preventDefault();
    const form = e.target;
    const data = {
      name: form.name.value
    }
    fetch(ENDPOINT + '/save', {
      method: 'POST',
      mode: 'cors',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    })
      .then(res => res.json())
      .then((data) => {
        console.log("Result", data);
        if (data.result !== "OK") {
          alert(data.result);
        } else {
          alert("↪Your file is saved 🖥🔥");
        }
      })
  }

  const handleLoad = (values, e) => {
    e.preventDefault();
    const form = e.target;
    const params = {
      animation: form.animation.value
    }
    fetch(ENDPOINT + '/load', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(params)
    })
      .then(res => res.json())
      .then((data) => {
        if (data.result.status === "new_snapshot") {
          snapshots.value = data.result.snapshot;
          return fetch('/load', {
            method: 'POST',
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(params)
          })
            .then(res => res.json())
        } else {
          return data
        }
      })
      .then((data) => {
        if (data.result.status === "OK") {
          console.log("Loading done", data);
          store.dispatch(clearAnimationSteps());
          store.dispatch(setMaxSteps(data.result.steps));
          return fetch(ENDPOINT + '/publish', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
          })
        } else {
          alert(data.result.message);
        }
      });
  }

  const handleDownload = (e) => {
    e.preventDefault()
    window.open("/video?shadows=0" + "&dt=" + (new Date()).getTime(), "_blank")
  }

  const handleAnimation = (event) => {
    setanimationlip(event.target.value);
  }

  const listAnimations = async (animationSelect) => {
    await fetch(ENDPOINT + '/list')
      .then(response => response.json())
      .then(data => {
        data.animations.forEach((text) => {
          setAnimation([...animation, ...data.animations]);
        })
      });
  }

  useEffect(() => {
    //listAnimations(animation);
  }, [])

  return(
    <>
    <div className="saveLoad">
    <button className="btn download" id="download-video" onClick={handleDownload}>Download Animation</button>

    {/* <form className="saveForm" key={2} id="save" onSubmit={handleSubmit2(handleSave)}> */}
      {/* <label className="label save">Save animation as:</label>
      <input className="input save" autoComplete="off" name="name" type="text" placeholder="type a name..." ref={register2} />
      <button className="btn save" name="save" type="submit" ref={register2}>Save</button> */}
    {/* </form> */}

    {/* <form className="loadForm" key={3} id="load" onSubmit={handleSubmit3(handleLoad)}>
      <FormControl className={classes.formControl}>
        <InputLabel className="inputNew" id="demo-simple-select-helper-label">Choose a Clip</InputLabel>
        <Select className="select load" autoComplete="off" name="animation"
          labelId="demo-simple-select-helper-label"
          id="demo-simple-select-helper"
          value={animationClip}
          onChange={handleAnimation}
        >
          {animation.map(value => (
            <MenuItem key={value} value={value} ref={register3}>{value}</MenuItem>
          ))}
        </Select>
        <FormHelperText>Load your saved animation</FormHelperText>
      </FormControl>
      <button className="btn load" name="load" type="submit" ref={register3}>Load</button>
    </form> */}
  </div>
  </>
  )
}
