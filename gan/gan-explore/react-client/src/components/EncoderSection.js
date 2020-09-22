import React, { useState, useEffect } from "react";
import ImageUploading from 'react-images-uploading';

export default function EncoderSection(props) { 
  const [images, setImages] = React.useState([]);
  const maxNumber = 6;

  const onChange = (imageList, addUpdateIndex) => {
    // data for submit
    console.log(imageList, addUpdateIndex);
    setImages(imageList);
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
              Click or Drop here
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
    </div>
  );
}