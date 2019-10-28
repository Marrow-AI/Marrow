import recognizeMicrophone from 'watson-speech/speech-to-text/recognize-microphone'

export default class WatsonListener {
    constructor() {
        console.log("Watson Listener constructed");
    }
    init(socket) {
        console.log("Init Watson Listener");
        this.socket = socket
        this.token = null
        this.listening = false;
        this.lastText = "";
        this.tokenCommand = "get-watson-token";
    }
    listen() {
        this.listening = true;
        console.log("WATSON LISTEN", this.token);
        this.stream = recognizeMicrophone({
            token: this.token,
            objectMode: true,
            format: false,
            profanity_filter: false,
            keepMicrophone: true,
            inactivity_timeout: -1
        });
        this.stream.on('data', (data) => {
            if (this.socket && this.socket.readyState == 1) {
                if (data.results[0]) {
                    let text = data.results[0].alternatives[0].transcript.replace("%HESITATION", "");
                    if (data.results[0].final) {
                        this.socket.send(JSON.stringify({action: 'speech', text: text}));
                    }
                    else if (text != this.lastText) {
                        this.socket.send(JSON.stringify({action: 'mid-speech', text: text}));
                        console.log(text);
                        this.lastText = text;
                    }
                }
            }
            else {
                console.log("Socket not connected!");
            }
        });
        this.stream.on('error', (err) => {
            console.log("ERROR!", err);
            this.stream.stop();
            this.stream.removeAllListeners('data');
            this.stream.removeAllListeners('error');
            if (this.listening) {
                // Here we go again
                this.listen();
            }
        });
    }
    stop() {
        if (this.stream) {
            this.stream.stop();
            this.stream.removeAllListeners('data');
            this.stream.removeAllListeners('error');
        }
        this.listening = false;
    }
}
