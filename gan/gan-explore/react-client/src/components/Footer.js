import React from 'react';
import { NavLink } from 'react-router-dom';
import Dialog from "@material-ui/core/Dialog";
import DialogActions from "@material-ui/core/DialogActions";
import DialogContent from "@material-ui/core/DialogContent";
import DialogContentText from "@material-ui/core/DialogContentText";
import DialogTitle from "@material-ui/core/DialogTitle";
import useMediaQuery from "@material-ui/core/useMediaQuery";
import { useTheme } from "@material-ui/core/styles";

const Footer = () => {
  const [open, setOpen] = React.useState(false);
  const theme = useTheme();
  const fullScreen = useMediaQuery(theme.breakpoints.down("sm"));

  const handleClickOpen = () => {
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
  };

  return (
    <>
      <div className="footerCointainer">
        <div className="mainFooter">
          <div className="footerDiv">

            <div>
              <button className='btn save footer' onClick={handleClickOpen}>About </button>
            </div>

            <div >
              <div className='logosdiv'>
                <a href='https://atlasv.io/' target="_blank"><img className='logos' src="/atlasV.png" alt='' /></a>
                <a href='https://www.nfb.ca/interactive/marrow' target="_blank"><img className='logos' src="/NFB.png" alt='' /></a>
                <a href='https://ars.electronica.art/news/de/' target="_blank"><img className='logos' src="/Ars-Electronica.png" alt='' /></a>
              </div>
            </div>

          </div>
        </div>
      </div>

      <Dialog
        fullScreen={fullScreen}
        open={open}
        onClose={handleClose}
        aria-labelledby="responsive-dialog-title"
      >
        <DialogTitle id="responsive-dialog-title">
          {"About GAN Latent Space Explorer Tool"}
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            who build it
            why we build it
            read more about it
            <br />
            <br />
            <a href="https://shirin.works/Marrow-teach-me-how-to-see-you-mother-Machine-learning-immersive" alt="" target="_blank" rel="noopener noreferrer">About Marrow</a><br />
            <a href="https://towardsdatascience.com/a-tool-for-collaborating-over-gans-latent-space-b7ea92ad63d8" alt="" target="_blank" rel="noopener noreferrer">About the tool</a>
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <button className='btn save footer' onClick={handleClose}> close </button>

        </DialogActions>
      </Dialog>
    </>
  );
};

export default Footer;
