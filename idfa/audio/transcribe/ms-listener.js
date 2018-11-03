export default class MSListener {
    constructor() {
        console.log("MS Listener constructed");
    }
    init(socket) {
        console.log("Init MS Listener");
        this.socket = socket
        this.token = null
        this.listening = false;
        this.tokenCommand = "get-token";
    }
    listen() {
        this.listening = true;
        console.log("LISTEN", this.token);
        let speechConfig = SpeechSDK.SpeechConfig.fromAuthorizationToken(this.token, "westus");
        speechConfig.language = "en-US";
        speechConfig.properties.setProperty('SpeechServiceResponse_RequestProfanityFilterTrueFalse', 'True');
        console.log(speechConfig.properties)
        let audioConfig  = SpeechSDK.AudioConfig.fromDefaultMicrophoneInput();
        this.recognizer = new SpeechSDK.SpeechRecognizer(speechConfig, audioConfig);

        //console.log(recognizer.properties)
        
        this.recognizer.startContinuousRecognitionAsync()
        this.recognizer.recognizing = (r,event) => {
            console.log('(' + event.result.text + ')');
            if (this.socket && this.socket.readyState == 1) {
                this.socket.send(JSON.stringify({action: 'mid-speech', text: event.result.text}));
            } else {
                console.log("Socket not connected!");
            }
        }
        this.recognizer.recognized = (r,event) => {
            if (event.result && event.result.text) {
                console.log(event.result.text);
                if (this.socket && this.socket.readyState == 1) {
                    this.socket.send(JSON.stringify({action: 'speech', text: event.result.text}));
                } else {
                    console.log("Socket not connected!");
                }
            }
        }
        this.recognizer.canceled = (r,event) => {
            console.log("Canceled!", event.reason, event.errorDetails);
            this.recognizer.stopContinuousRecognitionAsync()
            if (this.listening) {
                setTimeout(() => {
                    this.recognizer.startContinuousRecognitionAsync()
                },0)
            }
        }
    }
    stop() {
        console.log("Stopping");
        if (this.recognizer) {
            this.recognizer.stopContinuousRecognitionAsync()
        }
        this.listening = false;
    }
}
