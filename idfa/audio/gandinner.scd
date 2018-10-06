//TXSystem1.start(0,"/home/avnerus/Projects/gandinner/tx_modular_sample",showAudioOptions: false);

TXSystem1.start(showAudioOptions: false);

(
//l = NetAddr(\localhost.asString, 57120);
OSCdef.new( \speechreload, { |data,time,source|
	"reloading speech".postln;
	data.postln;
	if (TXSamplePlayer5a.arrInstances.size > 0) {
			TXSamplePlayer5a.arrInstances[0].loadSample(1);
	}
}, '/speech');
)


l.sendMsg("/speech", 111);