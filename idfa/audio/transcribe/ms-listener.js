export default class MSListener {
    constructor() {
        console.log("MS Listener constructed");
    }
    init(socket) {
        console.log("Init MS Listener");
        this.socket = socket
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
            console.log("Canceled!", event.reason, event.errorDetails);
            recognizer.startContinuousRecognitionAsync()
        }
    }
}
