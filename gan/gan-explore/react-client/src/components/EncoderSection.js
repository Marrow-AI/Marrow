import React, { useState } from "react";
import ImageUploading from 'react-images-uploading';
import store from '../state';
import { useSelector } from 'react-redux';

export default function EncoderSection(props) { 
  const dataset = useSelector(state => state.dataset);
  const [images, setImages] = useState([]);
  const maxNumber = 1;
  // const [imageUploaded, setImageUploaded] = useState(false);


  const onChange = (imageList, addUpdateIndex) => {
    console.log(imageList, addUpdateIndex);
    store.dispatch({
      type: 'SAVE_FILE_NAME',
      file_name: images
    })
    setImages(imageList);
  };

  const onSubmit = () => {
    console.log("Submitting image for encoding!", images)
    fetch('/encode', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({data:images[0].data_url, fileName: images[0].file.name})
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

  return (
    <div className="fileUploader">
      
      <div>
        <h1>{dataset}</h1>
      </div>
     
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
            <button className="btn generate"
              style={isDragging ? { color: 'red' } : undefined}
              onClick={onImageUpload}
              {...dragProps}
            > Upload your image
              
            </button>
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
       <button className="btn load encode" name="encode" onClick={onSubmit} >Encode Image to Space</button>
      </div> 
 
    </div>
  );
}
