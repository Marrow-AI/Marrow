import React, { useState, useEffect, useRef } from "react";
import { useForm } from "react-hook-form";

export default function Generate() {
  const [ view, setView ] = useState();
  const { register, errors, handleSubmit, } = useForm();
  const snapFamily = [
    "final", "000140", "001283", "002364", "003285", "004085", "004705", "005306", "005726", "006127", "006528",
    "006840", "007141", "007442", "007743", "008044", "008344", "008645", "008946", "009247", "009548", "009848",
    "010149", "010450", "010751", "011052", "011352", "011653", "011954", "012255", "012556", "012856", "013157",
    "013458", "013759", "014060", "014360", "014661", "014962", "015263", "015564", "015864", "016165", "016466",
    "016767", "017068", "017368", "017669", "017970", "018271", "018572", "018872", "019173", "019474", "019775",
    "020076", "020376", "020677", "020978", "021279", "021580", "021880", "022181", "022482", "022783", "023084",
    "023384", "023685", "023986", "024287", "024588", "024888", "025000"
  ];
 
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
      headers: {'Content-Type': 'application/json'},
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
        // loading.style.display = "none";
        setView("data:image/jpeg;base64," + data.result);
        console.log(data.data)
      }).catch(err => {
        console.log("Error Reading data " + err);
      })
  }

  useEffect(() => {

  })

  return (
    <>
      <div>
        <form id="shuffle" onSubmit={handleSubmit(onSubmit)}>
          {errors.singleErrorInput && "Your input is required"}

          <select name="type" autoComplete="off" ref={register}>
            <option value="person" defaultValue="selected" >Person</option>
            <option value="happy" >Happy families</option>
            <option value="cats" >cats</option>
          </select>

          <label >Snapshot:</label>
            <select name="snapshot" ref={register} >
            {snapFamily.map(value => (
            <option key={value} value={value} >{value}</option>
          ))}
          </select>

          <select name="shffle" autoComplete="off" ref={register}>
            <option value="both" defaultValue="selected" >Shuffle both source and destination</option>
            <option value="keep_source" >Keep source and shuffle destination</option>
            <option value="use_dest">Use destination as the next source</option>
          </select>

          <label>Number of steps:</label>
          <input autoComplete="off" name="steps" placeholder="144" min="1" type="number" ref={register} />

          <button type="onSubmit" ref={register}>Generate animation</button>
        </form>
      </div>

      <div id="output-container">
        <img src={view} width="512" height="512" alt=""/>
      </div>

      <div id="controls-container">
        <button className="generate" data-direction="back" data-steps="1">&lt;</button>
        <button className="generate" data-direction="forward" data-steps="1">&gt;</button>
        <button className="generate" data-direction="back" data-steps="10">&lt;10</button>
        <button className="generate" data-direction="forward" data-steps="10">10&gt;</button>
        <button className="generate" data-direction="back" data-steps="100">&lt;100</button>
        <button className="generate" data-direction="forward" data-steps="100">100&gt;</button>
      </div>
    </>
  );
}