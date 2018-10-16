export default class MSListener {
    constructor() {
        console.log("MS Listener constructed");
    }
    init() {
        console.log("Init MS Listener");
        this.socket = new WebSocket("ws://127.0.0.1:5678/");
        this.socket.onopen = (event) => {
            this.socket.send(JSON.stringify({action: 'get-token'}));
        }

        this.socket.onmessage = (packet) => {
            let message = JSON.parse(packet.data);
            console.log("Message",message)
            if (message.token) {
                this.listen(message.token)
            }

        }
    }
    listen(authorizationToken) {
        let speechConfig = SpeechSDK.SpeechConfig.fromAuthorizationToken(authorizationToken, "westus");
        speechConfig.language = "en-US";
        speechConfig.properties.setProperty('SpeechServiceResponse_RequestProfanityFilterTrueFalse', 'True');
        console.log(speechConfig.properties)
        let audioConfig  = SpeechSDK.AudioConfig.fromDefaultMicrophoneInput();
        let recognizer = new SpeechSDK.SpeechRecognizer(speechConfig, audioConfig);

        //console.log(recognizer.properties)
        
        recognizer.startContinuousRecognitionAsync()
        recognizer.recognizing = (r,event) => {
            console.log('(' + event.result.text + ')');
        }
        recognizer.recognized = (r,event) => {
            if (event.result && event.result.text) {
                console.log(event.result.text);
                this.socket.send(JSON.stringify({action: 'speech', text: event.result.text}));
            }
        }
        recognizer.canceled = (r,event) => {
            console.log("Canceled!");
            recognizer.startContinuousRecognitionAsync()
        }
    }
}
