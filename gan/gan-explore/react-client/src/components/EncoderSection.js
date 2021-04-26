import React, { useState, useEffect } from "react";
import ImageUploading from 'react-images-uploading';
import store from '../state';
import { useSelector } from 'react-redux';
import useSpinner from './useSpinner';
import Tree from 'react-d3-tree';
import { hierarchy, tree as d3Tree } from "d3";
import { cloneDeep, uniqueId } from 'lodash';

export default function EncoderSection(props) {
  const dataset = useSelector(state => state.dataset);
  const [loading, showLoading, hideLoading] = useSpinner();
  const ENDPOINT = useSelector(state => state.ENDPOINT);
  const [images, setImages] = useState([]);
  const currentStep = useSelector(state => state.currentStep);
  const currentShuffle = useSelector(state => state.currentShuffle);
  const snapshot = useSelector(state => state.snapshot);
  const maxSteps = useSelector(state => state.maxSteps);
  const [isGenerating, setIsGenerating] = useState(true);
  const animationSteps = useSelector(state => state.animationSteps);
  const finalDestination = useSelector(state => state.finalDestination);
  const [tree, setTree] = useState({
    name: 'root',
    children: [],
    attributes: {
      uuid: "ROOT"
    }
  });

  const [currentParent, setCurrentParent] = useState("ROOT");

  const renderRectSvgNode = ({ nodeDatum, toggleNode }) => (
    <g>
      {nodeDatum.attributes?.image ? (
        <>
          <defs>
            <pattern id={`image-${nodeDatum.attributes.uuid}`} x="0" y="0" viewBox="0 0 1024 1024" height="100%" width="1">
              <image x="0" y="0" width="1024" height="1024" href={nodeDatum.attributes.image}></image>
            </pattern>
          </defs>
          <circle r="35" onClick={toggleNode} fill={`url(#image-${nodeDatum.attributes.uuid}`} stroke='#3F51B5' />
        </>
      ) : (
        <>
          <circle r="5" fill='#000' stroke='transparent' onClick={toggleNode} />
        </>
      )}

    </g>
  );

  const addLastChild = (data) => {
    const treeClone = cloneDeep(tree);
    const root = hierarchy(treeClone);
    const descendants = root.descendants();
    const lastChild = descendants[descendants.length - 1].data;
    // if (currentStep === (maxSteps - 1)) {
    lastChild.children = [
      ...lastChild.children,
      {
        name: data.name,
        attributes: {
          image: data.imageUrl,
          uuid: uniqueId()
        }, children: []
      }
    ]
    // } if (currentStep < (maxSteps - 1)) {

    // }
    console.log("Tree", treeClone);
    setTree(treeClone);
    return lastChild.uuid;
  }

  const addChild = (data, parentId) => {
    const treeClone = cloneDeep(tree);
    const root = hierarchy(treeClone);
    console.log("Looking for parent", parentId);
    const parent = root.find((node) => node.data.attributes.uuid === parentId)
    console.log("Found parent", parent);
    const newId = uniqueId();
    parent.data.children = [
      ...parent.data.children,
      {
        name: data.name,
        attributes: {
          image: data.imageUrl,
          uuid: newId,
        }, children: []
      }
    ]
    // } if (currentStep < (maxSteps - 1)) {

    // }
    console.log("Tree", treeClone);
    setTree(treeClone);
    return newId;
  }


  const addBetween = (data) => {
    const treeClone = cloneDeep(tree);
    const root = hierarchy(treeClone);
    const descendants = root.descendants();
    const lastChild = descendants[descendants.length - 1];
    const parent = lastChild.parent;

    const between = {
      name: data.name,
      attributes: {
        image: data.imageUrl,
        uuid: uniqueId()
      }, children: [ lastChild.data ]
    }

    parent.data.children = [between];

    console.log("Tree", treeClone);
    setTree(treeClone);
    return between.attributes.uuid;
  }

  const onSubmit = () => {
    setIsGenerating(true)
    let nextParent;
    if (currentStep < maxSteps -1) {
      nextParent = addBetween({
        name: currentStep,
        imageUrl: animationSteps[currentStep]
      })
      setCurrentParent(nextParent);
    }

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
          showLoading();
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
    setImages(images => [...images, ...imageList]);
    console.log(images);

    console.log("Submitting image for encoding!", imageList)
    fetch(ENDPOINT + '/encode', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: imageList.map((image, i) => (
        JSON.stringify({
          data: image.data_url,
          fileName: image.file.name
        })
      ))
    })
      .then(res => res.json())
      .then((data) => {
        if (data.result === "OK") {
          showLoading();
          console.log("Result!", data)
        } else {
          alert(data.result);
        }
      })
  };

  useEffect(() => {
    if (currentStep === (maxSteps - 1)) {
      setIsGenerating(false)
      const childId = addChild({
        name: '',
        imageUrl: animationSteps[currentStep]
      }, currentParent)
      setCurrentParent(childId);
      hideLoading()
    }
  }, [currentStep])


  return (
    <div className="fileUploader">
      <div>
        <h1>{dataset}</h1>
      </div>
      <div className="mainSection" >
        {loading}
        <div className='encodeRandom'>
          <div className="encoderSection">
            <button disabled={isGenerating} className="btn generate" name="generate" type="onSubmit"
              onClick={onSubmit}>Generate Randomly</button>

            <ImageUploading
              value={images}
              onChange={onChange}
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
                </div>
              )}
            </ImageUploading>
          </div>
        </div>
      </div>
      <div id="treeWrapper" style={{ width: '50em', height: '10em', marginLeft: '30%', marginTop: '-12%', position: 'relative' }}>
        <Tree
          data={tree}
          renderCustomNodeElement={renderRectSvgNode}
        />
      </div>
    </div>
  );
}
