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
    }
}
