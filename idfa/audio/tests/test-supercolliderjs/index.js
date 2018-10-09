let sc = require('supercolliderjs');

sc.lang.boot({sclang: '/usr/bin/sclang'}).then(function(sclang) {

  sclang.interpret('TXSamplePlayer5a.arrInstances[0].loadSample(1);')
    .then(function(result) {
      // result is a native javascript array
      console.log('= ' + result);
    }, function(error) {
      // syntax or runtime errors
      // are returned as javascript objects
      console.error(error);
    });

}, function(error) {
  console.error(error)
  // sclang failed to startup:
  // - executable may be missing
  // - class library may have failed with compile errors
});
