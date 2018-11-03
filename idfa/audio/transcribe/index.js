import MSListener from './ms-listener'
import WatsonListener from './watson-listener'

$(document).ready(() => {
    console.log("Hello Marrow");

    $(".gain-input input").on('input', () => {
        updateGains();
    })
    $(".mood-input").on('input', () => {
        updateMood();
    })

    $("#control button").click((e) => {
        let command = $(e.target).data("command");
        console.log("Command! " + command);
        socket.send(JSON.stringify({action: 'control',command: command}));
    });
});

// Connecting to server
//const listener = new WatsonListener();
const listener = new MSListener();
const socket = new ReconnectingWebSocket("wss://localhost:9540/");
listener.init(socket);

socket.onopen = (event) => {
    socket.send(JSON.stringify({action: listener.tokenCommand}));
}
socket.onclose = (event) => {
    listener.stop();
}
socket.onmessage = (packet) => {
    let message = JSON.parse(packet.data);
    if (message.token) {
        listener.token = message.token;
        if (!listener.listening) {
            listener.listen()
        }
        if (listener instanceof MSListener) {
            // Microsoft renewal
            console.log("Renewing token in 9 minutes");
            setTimeout(() => {
                socket.send(JSON.stringify({action: 'get-token'}));
            },1000 * 60 * 9)
        }
    }
    else if (message.action == "pause") {
        console.log("Pause listening!", message.seconds);
        listener.stop();
        setTimeout(() => {
            listener.listen();            
        },message.seconds * 1000)
    }
    else if (message.action == "emotion") {
        updateEmotion(message.data, message.state)
    }
}

function updateEmotion(data, state) {
    //console.log("Update emotion", data, state);
    $("#vad-state").find("b").text(data.status)
    $("#vad-state").removeClass($("#vad-state").attr("class"))
    $("#vad-state").addClass(data.status);

    let tables = {0: "#arousal-table", 1: "#valence-table"};

    for (let tableIndex in tables) {
        let table = $(tables[tableIndex]).find("td");
        for (let i = 0; i < 3; i++) {
            if (data.analysis) {
                $(table[i+1]).text(Number((data.analysis[tableIndex][i]).toFixed(4)));
            } else {
                $(table[i+1]).text("--");
            }
        }
    }

   $($("#mood-average").find("td")[1]).text(Number((state).toFixed(3)));
   $("#mood-input input").val(Number((state).toFixed(3)));
   $("#mood-input span").text((state).toFixed(3));

   //$("#script-match").find("span").text(Number((state.script_match).toFixed(3)));

}

function updateGains() {
    let min = $("#gain-input-min input").val();
    let max = $("#gain-input-max input").val();

    $("#gain-input-min span").text(min);
    $("#gain-input-max span").text(max);

    socket.send(JSON.stringify({action: 'update-gain', min: min, max: max}));
}
function updateMood() {
    let mood = $("#mood-input input").val();

    $("#mood-input span").text(mood);

    socket.send(JSON.stringify({action: 'update-mood', value: mood}));
}

