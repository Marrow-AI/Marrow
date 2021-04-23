import React, { useState, useEffect, useRef }  from 'react';

function NiceLoader({ children, isLoading, ...props }) {
  /* Capture the dimensions of the button before the loading happens
  so it doesn’t change size when showing the loader */
  const [width, setWidth] = useState(0);
  const [height, setHeight] = useState(0);
  const ref = useRef(null);
  const [showLoader, setShowLoader] = React.useState(false);

  // Save the dimensions here
  useEffect(
    () => {
      if (ref.current && ref.current.getBoundingClientRect().width) {
        setWidth(ref.current.getBoundingClientRect().width);
      }
      if (ref.current && ref.current.getBoundingClientRect().height) {
        setHeight(ref.current.getBoundingClientRect().height);
      }
    },
    // children are a dep so dimensions are updated if initial contents change
    [children]
  );

  React.useEffect(() => {
    if (isLoading) {
      setShowLoader(true);
    }
  
    // Show loader a bits longer to avoid loading flash
    if (!isLoading && showLoader) {
      const timeout = setTimeout(() => {
        setShowLoader(false);
      }, 400);
  
      // Don’t forget to clear the timeout
      return () => {
        clearTimeout(timeout);
      };
    }
  }, [isLoading, showLoader]);

  return (
    <button
      className="button"
      ref={ref}
      style={
        width && height
          ? {
              width: `${width}px`,
              height: `${height}px`,
            }
          : {}
      }
      {...props}
    >
      {showLoader ? <Loader /> : children}
    </button>
  );
}

export default NiceLoader();