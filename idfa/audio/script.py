import spacy
import numpy as np
from annoy import AnnoyIndex
import json

SCRIPT_TIMEOUT_GLOBAL = 99999 #11
SCRIPT_TIMEOUT_NOSPEECH = 99999 #6 

SCRIPT_TIMEOUT_GLOBAL_SHORT = 99999 #9
SCRIPT_TIMEOUT_NOSPEECH_SHORT = 99999 #6

class Script:
    def __init__(self):
        print("Initializing script engine")
        self.nlp = spacy.load('en_core_web_sm')
        self.awaiting_index = -1
        self.load_data()

    def load_data(self):
        with open("marrow_script.json", 'r') as file:
            self.data = json.load(file)

    def reset(self):
        self.load_data()
        self.awaiting_index = 0
        self.awaiting = self.data["script-lines"][self.awaiting_index]
        self.update()
        self.length = len(self.data["script-lines"])

    def update(self):
        if "text" in self.awaiting:
            self.awaiting_text = self.awaiting["text"]
            self.awaiting_variation = 0
            self.awaiting["words"] = len(self.awaiting_text.split())
            print("AWAITING: {}".format(self.awaiting_text))
            self.awaiting_nlp = self.nlp(self.awaiting_text)
            if self.awaiting_index > 0 and "text" in self.data["script-lines"][self.awaiting_index -1] :
                self.awaiting["previous"] = self.data["script-lines"][self.awaiting_index -1]["text"]
            if "timeout" in self.awaiting:
                self.awaiting_nospeech_timeout = SCRIPT_TIMEOUT_NOSPEECH_SHORT
                self.awaiting_global_timeout = SCRIPT_TIMEOUT_GLOBAL_SHORT
            else:
                self.awaiting_nospeech_timeout = SCRIPT_TIMEOUT_NOSPEECH
                self.awaiting_global_timeout = SCRIPT_TIMEOUT_GLOBAL

            # Add some more to the first lines after returning from gan
            if "timeout-response" in self.awaiting:
                self.awaiting_nospeech_timeout += 2
                self.awaiting_global_timeout += 2
        else:
            self.awaiting_text = None
            self.awaiting_global_timeout = None
        if "type" in self.awaiting:
            self.awaiting_type = self.awaiting["type"]
        else:
            self.awaiting_type = "line"
        

    def next_variation(self):
        if ("variations" in self.awaiting and len(self.awaiting["variations"]) - 1 >= self.awaiting_variation):
            self.awaiting_text = self.awaiting["variations"][self.awaiting_variation]
            self.awaiting_variation += 1
            self.update()
            return True
        else:
            return False

    def next_line(self):
        if self.awaiting_index < self.length - 1:
            self.awaiting_index = self.awaiting_index + 1
            self.awaiting_variation = 0
            self.awaiting = self.data["script-lines"][self.awaiting_index]
            self.update()

            return True
        else:
            return False

    def prev_line(self):
        if self.awaiting_index > 0:
            self.awaiting_index = self.awaiting_index - 1
            self.awaiting_variation = 0
            self.awaiting = self.data["script-lines"][self.awaiting_index]
            self.update()

            return True
        else:
            return False

    def load_space(self):
        self.script_lines = {}

        dimensions = self.meanvector("I like apples").shape[0]
        print ("{} dimensions".format(dimensions))

        self.text_space = AnnoyIndex(dimensions, metric='angular')

        i = 0
        line_i = 0
        inserted_lines = list()
        for line in self.data["script-lines"]:
            text = line["text"]
            try:
                mean_vector = self.meanvector(text)
                self.text_space.add_item(i, mean_vector)
                inserted_lines.append(line)
                i += 1
            except IndexError:
                print('NLP error at "{}"'.format(text))
                continue

            finally:
                line_i += 1

        self.text_space.build(10)
        print("{} items in vector space for {} lines".format(self.text_space.get_n_items(), len(inserted_lines)))
        assert(self.text_space.get_n_items() == len(inserted_lines))


    def meanvector(self,text):
        s = self.nlp(text)
        vecs = [word.vector for word in s \
                if word.pos_ in ('NOUN', 'VERB', 'ADJ', 'ADV', 'PROPN', 'ADP') \
                and np.any(word.vector)] # skip all-zero vectors
        if len(vecs) == 0:
            raise IndexError
        else:
            return np.array(vecs).mean(axis=0)

    def match(self,text):
        try:
            text_nlp = self.nlp(text)
            distance =  self.awaiting_nlp.similarity(text_nlp)
            print("{} => {}".format(text_nlp,distance))
            return distance
        except Exception as e:
            print("Exception {}".format(e))
            return 0

    def match_space(self,text):
        try:
            nearest = self.text_space.get_nns_by_vector(
                    self.meanvector(text),
                    n=2,
                    include_distances=True
            )

            matches = []
            for i in range(0, len(nearest[0])):
                matches.append({
                    "index": nearest[0][i],
                    "distance": nearest[1][i]
                 })

            return matches
        except:
            return None
