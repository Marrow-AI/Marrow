//TXSystem1.start(0,"/home/avnerus/Projects/gandinner/tx_modular_sample",showAudioOptions: false);


(
OSCdef.new( \speechreload, { |data,time,source|
	"reloading speech".postln;
	data.postln;
	if (TXSamplePlayer5a.arrInstances.size > 0) {
			"foundsample!".postln;
		    TXSamplePlayer5a.arrInstances[0].loadSample(0);
			TXSamplePlayer5a.arrInstances[0].loadSample(1);
	}
}, '/speech/reload');

)

o = ServerOptions.new;
o.device = "ASIO : US-2x2 & US-4x4 Mixing Driver";
t = Server(\Local,NetAddr("127.0.0.1", 57111),o);
t.boot;

(
OSCdef.new( \speechload, { |data,time,source|
	"loading speech".postln;
	data.postln;
	if (TXFilePlayer6.arrInstances.size > 0) {
			"foundplayer!".postln;
		    TXFilePlayer6.arrInstances[0].cueSample(data[1].asString);
	}
}, '/speech/load');



OSCdef.new( \effectload, { |data,time,source|
	"loading speech".postln;
	data.postln;
	if (TXFilePlayer6St.arrInstances.size > 0) {
			"foundplayer!".postln;
		    TXFilePlayer6St.arrInstances[0].cueSample(data[1].asString);
	}
}, '/effect/load');


//TXSystem1.start(showAudioOptions: false)
TXSystem1.start(0,"C:/Marrow/idfa/audio/TX_Modular/tx_marrow_123",showAudioOptions: false);
)
;

a = NetAddr.new("127.0.0.1", 57120);
a.sendMsg("/control/musicbox", 0.0, 0.6, 0.0, 0.5);
a.sendMsg("/control/bells", 0.0, 0.22);
a.sendMsg("/control/bassheart", 0.0, 0.9);
a.sendMsg("/control/membrane", 0.7, 0.45,0.2);
a.sendMsg("/control/synthbass", 0.0, 0.4,0.0);
a.sendMsg("/synthbass/effect", 0.0);
a.sendMsg("/strings/effect", 3, 0.8);
a.sendMsg("/control/strings", 0.0, 1.0);
a.sendMsg("/gan/echo", 2);

Server.default.record;
Server.default.stopRecording

Platform.userConfigDir.postln;

