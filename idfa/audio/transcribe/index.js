$(document).ready(() => {
    console.log("Hello Marrow");

    $("#control button").click((e) => {
        let command = $(e.target).data("command");
        console.log("Command! " + command);
        socket.send(JSON.stringify({action: 'control',command: command}));
    });
    //
    // Connecting to server
    //const listener = new WatsonListener();
    const socket = new ReconnectingWebSocket("ws://localhost:9540/");

    socket.onopen = (event) => {
        console.log("Socket opened");
    }
    socket.onclose = (event) => {
        console.log("Socket closed.");
    }
});


