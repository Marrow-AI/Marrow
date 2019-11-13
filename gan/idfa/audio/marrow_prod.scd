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
)

(
OSCdef.new( \effectload, { |data,time,source|
	"loading speech".postln;
	data.postln;
	if (TXFilePlayer6St.arrInstances.size > 0) {
			"foundplayer!".postln;
		    TXFilePlayer6St.arrInstances[0].cueSample(data[1].asString);
	}
}, '/effect/load');
)
(
o = ServerOptions.new;
o.device = "ASIO : US-2x2 & US-4x4 Mixing Driver";
t = Server(\Local,NetAddr("127.0.0.1", 57111),o);
t.boot;

//TXSystem1.start(showAudioOptions: false)
 TXSystem1.start(1,"TX_Modular/tx_marrow_123",showAudioOptions: false);

)
;