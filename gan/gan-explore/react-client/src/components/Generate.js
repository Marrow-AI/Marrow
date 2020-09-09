import React, { useState, useEffect } from "react";
import { useForm } from "react-hook-form";
import snapshots from './snapshots.json';
import Footer from './Footer.js';
import { makeStyles } from '@material-ui/core/styles';
import InputLabel from '@material-ui/core/InputLabel';
import MenuItem from '@material-ui/core/MenuItem';
import FormHelperText from '@material-ui/core/FormHelperText';
import FormControl from '@material-ui/core/FormControl';
import Select from '@material-ui/core/Select';
import RootRef from '@material-ui/core/RootRef';

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

export default function Generate() {
  const domRef = React.useRef();

  const [view, setView] = useState();
  const [animation, setAnimation] = useState([]);
  const { register, handleSubmit } = useForm({ mode: "onBlur" });
  const { register: register2, handleSubmit: handleSubmit2 } = useForm({ mode: "onBlur" });
  const { register: register3, handleSubmit: handleSubmit3 } = useForm({ mode: "onBlur" });
  const classes = useStyles();
  const [dataset, setDataset] = React.useState('');
  const [snapshot, setSnapshot] = React.useState('');
  const [generating, setGenerating] = React.useState('');
  const [animationClip, setanimationlip] = React.useState('');

  const handleChange = (event) => {
    setDataset(event.target.value);
  };

  const handleSnapshot = (event) => {
    setSnapshot(event.target.value);
  }

  const handleGenerating = (event) => {
    setGenerating(event.target.value);
  }

  const handleAnimation = (event) => {
    setanimationlip(event.target.value);
  }

  const onSubmit = (values, ev) => {
    const form = ev.target;
    const data = {
      steps: form.steps.value,
      snapshot: "007743",
      type: form.type.value
    }
    console.log(data)
    fetch('http://localhost:8080/shuffle', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    })
      .then(res => res.json())
      .then((data) => {
        if (data.result === "OK") {
          return getImage("forward", "1");
        } else {
          alert(data.result);
        }
      })
  };

  const getImage = async (direction, steps) => {
    await fetch('http://localhost:8080/generate?direction=' + direction + '&steps=' + steps + '&shadows=0')
      .then(response => response.json())
      .then(data => {
        setView("data:image/jpeg;base64," + data.result)
      }).catch(err => {
        console.log("Error Reading data " + err);
      })
  }

  const listAnimations = async (animationSelect) => {

    await fetch('http://localhost:8080/list')
      .then(response => response.json())
      .then(data => {
        data.animations.forEach((text) => {
          setAnimation([...animation, ...data.animations]);
        })
      });
  }

  const handleDirection = (e) => {
    e.preventDefault()
    getImage(
      e.currentTarget.dataset.direction,
      e.currentTarget.dataset.steps
    )
  }

  const handleSave = (values, e) => {
    e.preventDefault();
    const form = e.target;
    const data = {
      name: form.name.value
    }
    fetch('http://localhost:8080/save', {
      method: 'POST',
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
    fetch('http://localhost:/load', {
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
          form.steps.value = parseInt(data.result.steps);
          return getImage("forward", "1")
        } else {
          alert(data.result.status);
        }
      });
  }

  const handleDownload = (e) => {
    e.preventDefault()
    window.open("/video?shadows=0" + "&dt=" + (new Date()).getTime(), "_blank")
  }

  useEffect(() => {
    listAnimations(animation);
  }, [])

  // useEffect(() => {
  //   console.log(domRef.current); // DOM node
  // }, []);

  return (
    <>
      <h1 className="secondTitle">EXPLORER TOOL</h1>
      <div className="main">
        <div className="mainSection">
          <form className="formLeft" key={1} className="shuffleForm" onSubmit={handleSubmit(onSubmit)} >
            <FormControl className={classes.formControl} >
              <InputLabel className="inputNew" id="demo-simple-select-helper-label">Choose a dataset</InputLabel>
              <Select className="select dataset" name="type" autoComplete="off"
                labelId="demo-simple-select-helper-label"
                id="demo-simple-select-helper"
                value={dataset}
                onChange={handleChange}
              >
                <MenuItem value={"person"}>This Person Does Not Exist</MenuItem>
                <MenuItem value={"happy"} >Happy Families Dinner</MenuItem>
              </Select>
              <FormHelperText>Load a dataset of your intreset</FormHelperText>
            </FormControl>

            <FormControl className={classes.formControl}>
              <InputLabel className="inputNew" id="demo-simple-select-helper-label" >Choose a Snapshot</InputLabel>
              <Select className="select snapshot" autoComplete="off"
                labelId="demo-simple-select-helper-label"
                id="demo-simple-select-helper"
                value={snapshot}
                onChange={handleSnapshot}
              >
                {snapshots.snapshots.snapFamily.map(value => (
                  <MenuItem className="snapshot" value={value} key={value} >{value}</MenuItem>
                ))}
              </Select>
              <FormHelperText>Load a number of Snapshot from the choosen Dataset</FormHelperText>
            </FormControl>

            <FormControl className={classes.formControl}>
              <InputLabel className="inputNew" id="demo-simple-select-helper-label">Generating Options</InputLabel>
              <Select className="select shuffle" name="shffle" autoComplete="off"
                labelId="demo-simple-select-helper-label"
                id="demo-simple-select-helper"
                value={generating}
                onChange={handleGenerating}
              >
                <MenuItem value={"both"} >Shuffle both source and destination</MenuItem>
                <MenuItem value={"keep_source"} >Keep source and shuffle destination</MenuItem>
                <MenuItem value={"use_dest"} >Use destination as the next source</MenuItem>
              </Select>
              <FormHelperText>Choose how to generate your animations</FormHelperText>
            </FormControl>

            <div className="stepsDiv">
              <label className="label steps"> Number of steps:</label>
              <input className="input steps" autoComplete="off" name="steps" placeholder="type a number..." min="1" type="number" />
            </div>

            <div className="divBtnGnr">
              <button className="btn generate" type="onSubmit" ref={register}>Generate animation</button>
            </div>
          </form>

          <div className="imgControler">
            <div className="output-container">
              <img className="imgAnimation" src={view} width="512" height="512" alt="" />
            </div>


            <div className="controls-container">
              <button onClick={handleDirection} className="direction" data-direction="back" data-steps="1">&lt;</button>1
              <button onClick={handleDirection} className="direction" data-direction="forward" data-steps="1">&gt;</button>
              <button onClick={handleDirection} className="direction" data-direction="back" data-steps="10">&lt;&lt;</button>10
              <button onClick={handleDirection} className="direction" data-direction="forward" data-steps="10">&gt;&gt;</button>
              <button onClick={handleDirection} className="direction" data-direction="back" data-steps="100">&lt;&lt;&lt;</button>100
              <button onClick={handleDirection} className="direction" data-direction="forward" data-steps="100">&gt;&gt;&gt;</button>
            </div>

            <div className="saveLoad">
              <form className="saveForm" key={2} id="save" onSubmit={handleSubmit2(handleSave)}>
                <label className="label save">Save animation as:</label>
                <input className="input save" autoComplete="off" name="name" type="text" placeholder="type a name..." ref={register2} />
                <button className="btn save" type="submit" ref={register2}>Save</button>
                <button className="btn download" id="download-video" onClick={handleDownload}>Download</button>
              </form>

              <form className="loadForm" key={3} id="load" onSubmit={handleSubmit3(handleLoad)}>

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

                <button className="btn load" type="submit" ref={register3}>Load</button>
              </form>
            </div>
          </div>
        </div>
      </div>
      <Footer />

    </>
  );
}