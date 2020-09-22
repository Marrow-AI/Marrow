const express = require ('express');
const http = require('http');
const cors = require('cors');
const axios = require('axios');
const socketIo = require('socket.io');
const index = require('./index');
const port = process.env.PORT || 4000;
// const URL ='https://randomuser.me/api/?page=3&results=5&seed=abc';

const app = express();
app.use(index);

const server = http.createServer(app);
const io = socketIo(server);
console.log('ws is running');


io.on('connection', newConnection);

function newConnection(socket) {
    console.log('new connection:' + socket.id);

    socket.on('disconnect', () => {
      console.log('Client disconnected:' + socket.id);
    })
  }

    server.listen(port, () => console.log(`Listening on port ${port}`));
