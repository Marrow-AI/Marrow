//TXSystem1.start(0,"/home/avnerus/Projects/gandinner/tx_modular_sample",showAudioOptions: false);

(
OSCdef.new( \speechreload, { |data,time,source|
	"reloading speech".postln;
	data.postln;
	if (TXSamplePlayer5a.arrInstances.size > 0) {
			"foundsample".postln;
			TXSamplePlayer5a.arrInstances[0].loadSample(1);
	}
}, '/speech/reload');

   TXSystem1.start(0,"TX_Modular/tx_marrow_44",showAudioOptions: false);
)


