import spacy
import numpy as np
from annoy import AnnoyIndex

class Script:
    def __init__(self):
        print("Initializing script")
        self.nlp = spacy.load('en')
        self.script_lines = {}

        dimensions = self.meanvector("I like apples").shape[0]
        print ("{} dimensions".format(dimensions))

        self.text_space = AnnoyIndex(dimensions, metric='angular')

        i = 0
        line_i = 0
        file = open("script.txt", "r")
        lines = file.readlines()
        inserted_lines = list()
        for line in lines:
            line = line.rstrip()    
            try:        
                mean_vector = self.meanvector(line)        
                self.text_space.add_item(i, mean_vector)
                inserted_lines.append(line)
                self.script_lines[i] = {"text": line, "index": line_i}
                i += 1
            except IndexError:
                print('NLP error at "{}"'.format(line))
                continue    

            finally:
                line_i += 1

        self.text_space.build(100)
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
            nearest = self.text_space.get_nns_by_vector(
                    self.meanvector(text), 
                    n=1, 
                    include_distances=True
            )
            line_index = nearest[0][0]
            return {"match": nearest[1][0], "line": self.script_lines[line_index]}
        except:
            return None


    
