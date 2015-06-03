# Parse Corpus and save as pickle
# Zhen Li, Tsinghua University.
import pickle

from constants import *
from keyboardLayoutHelper import *
from filesHelper import *

# Main

wordDic = {}
wordCnt = {}

# Parse the corpus
corpusSize = -1
allWords = loadCorpus(-1)

for word in allWords:
    # print word + ' : ' + encode(word)
    code = encode(word)
    if {code}.issubset(wordDic.keys()):
        wordDic[code].add(word)
    else:
        wordDic[code] = set()
        wordDic[code].add(word)
        
    if {word}.issubset(wordCnt.keys()):
        wordCnt[word] += 1
    else:
        wordCnt[word] = 1

corpusFile = file('corpus_full_size.pkl', 'wb')
pickle.dump(wordDic, corpusFile, True)
pickle.dump(wordCnt, corpusFile, True)