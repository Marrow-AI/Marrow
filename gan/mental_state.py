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
        if analysis[0][0] >= 0.8: # Low arousal
            self.value = max(0, self.value - 0.05 * analysis[0][0])
        else : # High arousal
            self.value = min(1, self.value + 0.05 * analysis[0][2])

        if analysis[1][0] >= 0.5: # Low valence
            self.value = max(0, self.value - 0.05 * analysis[1][0])
        else: # High valence
            self.value = min(1, self.value + 0.1 * analysis[1][2])

        return
