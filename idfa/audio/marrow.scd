//TXSystem1.start(0,"/home/avnerus/Projects/gandinner/tx_modular_sample",showAudioOptions: false);

TXSystem1.start(showAudioOptions: false);

(
OSCdef.new( \speechreload, { |data,time,source|
	"reloading speech".postln;
	data.postln;
	if (TXSamplePlayer5a.arrInstances.size > 0) {
			"foundsample".postln;
			TXSamplePlayer5a.arrInstances[0].loadSample(1);
	}
}, '/speech/reload');
)


