import React, {Component} from 'react';
import './styles/Credit.css';

class Credit extends Component {

  render() {
    const {title, name, link, large, name2, link2} = this.props;
    return (
      <div className={large ? 'Credit-Large' : 'Credit'}>
        <div className='Credit-Component'>
          <div className='Credit-Title'>
            {title} 
          </div>
          <div className='Credit-Name'>
            <a href={link} target='_blank' rel="noopener noreferrer">{name} </a>
            <a href={link2} target='_blank' rel="noopener noreferrer"> {name2}</a>
          </div>
        </div>
      </div>
    )
  }
}

export default Credit;
