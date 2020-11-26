import React, { useState } from "react";
import ImageUploading from 'react-images-uploading';
import store from '../state';

export default function EncoderSection(props) { 
  const [images, setImages] = useState([]);
  const maxNumber = 6;

  const onChange = (imageList, addUpdateIndex) => {
    // data for submit
    console.log(imageList, addUpdateIndex);
    store.dispatch({
      type: 'SAVE_FILE_NAME',
      file_name: images
    })
    setImages(imageList);
  };

  const onSubmit = () => {
    // data for submit
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
          onImageRemoveAll,
          onImageUpdate,
          onImageRemove,
          isDragging,
          dragProps,
        }) => (
          // write your building UI
          <div className="upload__image-wrapper">
            <button className="btn load-images"
              style={isDragging ? { color: 'red' } : undefined}
              onClick={onImageUpload}
              {...dragProps}
            >
              Click or Drop here to add an Image 
            </button>
            &nbsp;
            <button className="btn load-images remove" onClick={onImageRemoveAll}>Remove all images</button>
            {imageList.map((image, index) => (
              <div key={index} className="image-item">
                <img src={image['data_url']} alt="" width="100" />
                {/* <div className="image-item__btn-wrapper">
                  <button className="btn load-images" onClick={() => onImageUpdate(index)}>Update</button>
                  <button className="btn load-images" onClick={() => onImageRemove(index)}>Remove</button>
                </div> */}
              </div>
            ))}
          </div>
        )}
      </ImageUploading>
      <div className="divBtnGnr">
        <button className="btn encode" name="encode" onClick={onSubmit} >Encode</button>
      </div>
    </div>
  );
}
