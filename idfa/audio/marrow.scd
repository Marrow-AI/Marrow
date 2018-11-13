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


TXSystem1.start(showAudioOptions: false)
)
;

a = NetAddr.new("127.0.0.1", 57120);
a.sendMsg("/control/membrane", 0.0, 0.5, 0.1);
a.sendMsg("/control/bassheart", 0.0, 0.9);

Server.default.record;
Server.default.stopRecording

