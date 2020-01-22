$(document).ready(() => {
    console.log("Hello Marrow");

    $("#control button").click((e) => {
        let command = $(e.target).data("command");
        console.log("Command! " + command);
        socket.send(JSON.stringify({action: 'control',command: command}));
    });
    $("#connected").hide();    	
    $("#disconnected").show();    	
    //
    // Connecting to server
    //const listener = new WatsonListener();
    const socket = new ReconnectingWebSocket("ws://192.168.1.22:9540/");

    socket.onopen = (event) => {
        console.log("Socket opened");
	$("#connected").show();    	
        $("#disconnected").hide();    	
    }
    socket.onclose = (event) => {
        console.log("Socket closed.");
	$("#connected").hide();    	
	$("#disconnected").show();    	
    }
});


