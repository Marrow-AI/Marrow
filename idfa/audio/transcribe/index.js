import MSListener from './ms-listener'

$(document).ready(() => {
    console.log("Hello Marrow");

    $(".gain-input input").on('input', () => {
        updateGains();
    })
});

// Connecting to server
const msListener = new MSListener();
const socket = new ReconnectingWebSocket("wss://localhost:9540/");

socket.onopen = (event) => {
    msListener.init(socket);
    socket.send(JSON.stringify({action: 'get-token'}));
}
socket.onmessage = (packet) => {
    let message = JSON.parse(packet.data);
    if (message.token) {
        msListener.listen(message.token)
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

   $($("#arousal-average").find("td")[1]).text(Number((state.arousal).toFixed(3)));
   $($("#valence-average").find("td")[1]).text(Number((state.valence).toFixed(3)));

   $("#script-match").find("span").text(Number((state.script_match).toFixed(3)));


   $("#mood").find("span").text(state.mood.toUpperCase());
}

function updateGains() {
    let min = $("#gain-input-min input").val();
    let max = $("#gain-input-max input").val();

    $("#gain-input-min span").text(min);
    $("#gain-input-max span").text(max);

    socket.send(JSON.stringify({action: 'update-gain', min: min, max: max}));
}

