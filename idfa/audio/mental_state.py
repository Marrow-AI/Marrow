import numpy as np

class MentalState:
    def __init__(self):
        print("Initializing mental state")

        self.valence = 0.5
        self.arousal = 0.5
        self.script_match = 0

    def get_current_state(self):
        state = {
            "valence": self.valence,
            "arousal": self.arousal,
            "script_match": self.script_match
        }
        if self.valence >= 0.5:
            if self.arousal >= 0.5: 
                state["mood"] = "happy"
            else:
                state["mood"] = "tired"
        elif self.arousal >= 0.5: 
            state["mood"] =  "angry"
        else:
            state["mood"] = "sad"

        return state

    def update_silence(self):
        self.arousal = max(0, self.arousal - 0.02)

    def update_script_match(self,match):
        self.script_match = 1 - match
        if self.script_match > 0.5:
            self.valence = min(1, self.valence + 0.2 * self.script_match)
        else:
            self.valence = max(0, self.valence - 0.1 * (1-self.script_match))


    def update_emotion(self,analysis):
        maxes = np.argmax(analysis,axis=1)
        if maxes[0] == 0: # Low arousal
            self.arousal = max(0, self.arousal - 0.05 * analysis[0][0])
        elif maxes[0] == 2: # High arousal
            self.arousal = min(1, self.arousal + 0.05 * analysis[0][2])

        if analysis[1][0] >= 0.5: # Low valence
            self.valence = max(0, self.valence - 0.05 * analysis[1][0])
        else: # High valence
            self.valence = min(1, self.valence + 0.1 * analysis[1][2])

        return
