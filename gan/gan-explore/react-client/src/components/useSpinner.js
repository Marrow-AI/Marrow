import React, { useState } from "react";
import Loading from "./Loaing";

const useSpinner = props => {
  const [visible, setVisible] = useState(false);
  
  const showLoading = () => setVisible(true);
  const hideLoading = () => setVisible (false);
  
  const loading = visible ? <Loading /> : null;

  return [loading, showLoading, hideLoading]
}

export default useSpinner;
