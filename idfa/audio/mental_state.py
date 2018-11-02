import numpy as np

class MentalState:
    def __init__(self):
        print("Initializing mental state")

        self.value = 0.5
        self.script_match = 0

    def get_current(self):
        return value

    def update_silence(self):
        return 
        #self.arousal = max(0, self.arousal - 0.02)

    def update_script_match(self,match):
        self.script_match = 1 - match
#        if self.script_match > 0.5:
#            self.valence = min(1, self.valence + 0.2 * self.script_match)
#        else:
#            self.valence = max(0, self.valence - 0.1 * (1-self.script_match))
        return


    def update_emotion(self,analysis):
        maxes = np.argmax(analysis,axis=1)
#        if maxes[0] == 0: # Low arousal
#            self.arousal = max(0, self.arousal - 0.05 * analysis[0][0])
#        elif maxes[0] == 2: # High arousal
#            self.arousal = min(1, self.arousal + 0.05 * analysis[0][2])
#
#        if analysis[1][0] >= 0.5: # Low valence
#            self.valence = max(0, self.valence - 0.05 * analysis[1][0])
#        else: # High valence
#            self.valence = min(1, self.valence + 0.1 * analysis[1][2])

        return
