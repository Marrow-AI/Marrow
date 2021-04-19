import React, { useState, useEffect } from "react";
import ImageUploading from 'react-images-uploading';
import store from '../state';
import { useSelector } from 'react-redux';
// import ShowEncodedImages from './ShowEncodedImages';

export default function EncoderSection(props) { 
  const dataset = useSelector(state => state.dataset);
  const ENDPOINT = useSelector(state => state.ENDPOINT);
  const [images, setImages] = useState([]);
  const maxNumber = 1;
  const currentStep = useSelector(state => state.currentStep);
  const currentShuffle = useSelector(state => state.currentShuffle);
  const snapshot = useSelector(state => state.snapshot);
  const maxSteps = useSelector(state => state.maxSteps);
  const [isGenerating, setIsGenerating] = useState(true)

  const onSubmit = () => {
  setIsGenerating(true)
  const data = {
    dataset: dataset,
    steps: maxSteps,
    snapshot: snapshot,
    type: currentShuffle,
    currentStep: currentStep
  }
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
  }

  const onChange = (imageList, addUpdateIndex) => {
    setIsGenerating(true)
    console.log(imageList, addUpdateIndex);
    store.dispatch({
      type: 'SAVE_FILE_NAME',
      file_name: images
    })
    setImages(imageList);
  
    console.log("Submitting image for encoding!", imageList)
    fetch(ENDPOINT + '/encode', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({data:imageList[0].data_url, fileName:imageList[0].file.name})
    })
    .then(res => res.json())
    .then((data) => {
      if (data.result === "OK") {
        console.log("Result!",data)
      } else {
        alert(data.result);
      }
    })
  };

  useEffect(()=> {
    if(currentStep === (maxSteps-1)) {
      setIsGenerating(false)
    }
    console.log(isGenerating)
    console.log(currentStep)
  }, [currentStep])

  return (
    <div className="fileUploader">
      <div>
        <h1>{dataset}</h1>
      </div>

      <div className='encodeRandom'>
      <button disabled={isGenerating} className="btn generate" name="generate" type="onSubmit" onClick={onSubmit}>Generate Randomly</button>

      <div className="encoderSection">
      <ImageUploading
        multiple
        value={images}
        onChange={onChange}
        maxNumber={maxNumber}
        dataURLKey="data_url"
      >     
        {({
          imageList,
          onImageUpload,
          isDragging,
          dragProps,
        }) => (
          
          <div className="upload__image-wrapper">
            <button disabled={isGenerating} className="btn generate"
              style={isDragging ? { color: 'red' } : undefined}
              onClick={onImageUpload}
              {...dragProps}> Upload your image </button>
            &nbsp;
            {imageList.map((image, index) => (
              <div key={index} className="image-item">
                <p>({image.file.name})</p>
                {/* <img src={image['data_url']} alt="" width="100" /> */}
              </div>
            ))}
          </div>
        )}
       </ImageUploading> 
       </div>
      </div> 
    </div>
  );
}
