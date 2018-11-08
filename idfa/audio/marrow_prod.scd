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

TXSystem1.start(0,"/home/avnerus/Projects/Marrow/idfa/audio/TX_Modular/tx_marrow_63",showAudioOptions: false);
)



