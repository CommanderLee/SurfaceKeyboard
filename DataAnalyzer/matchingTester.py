# Test different algorithms for word matching.
# Zhen Li, Tsinghua University.
import csv
import matplotlib.pyplot as plt
from pylab import *
import Tkinter, tkFileDialog
import numpy as np 

def encode(string):
    "Encode the word using handCode rules."
    code = ''
    for char in string:
        code += handCode[ord(char) - ord('a')]
    return code

# Main

# Read word list
textFile = open('TaskText.txt', 'r')
texts = [text.strip() for text in textFile]
# print texts

# Pre-processing: Encode the words. 0:left, 1:right.
# a b c d e f g
# h i j k l m n
# o p q r s t u v w x y z
handCode = ['0', '0', '0', '0', '0', '0', '0', 
'1', '1', '1', '1', '1', '1', '1',
'1', '1', '0', '0', '0', '0', '1', '0', '0', '0', '1', '0']

wordDic = {}
wordCnt = {}
for sentence in texts:
    for word in sentence.lower().split(' '):
        # print word + ' : ' + encode(word)
        code = encode(word)
        if {code}.issubset(wordDic):
            wordDic[code].add(word)
        else:
            wordDic[code] = set()
            wordDic[code].add(word)
            
        if {word}.issubset(wordCnt):
            wordCnt[word] += 1
        else:
            wordCnt[word] = 1

print wordDic
print wordCnt

# Number of words sharing the same code
sameCodeCnt = [len(wordSet) for wordSet in wordDic.values()]
print 'Number of words sharing the same code:\n    Max:%d, Mean:%d, Median:%d' \
% (np.max(sameCodeCnt), np.mean(sameCodeCnt), np.median(sameCodeCnt))

# Frequency of words
wordNum = wordCnt.values()
wordSum = np.sum(wordNum)
print 'Word Frequency:\n    Sum:%d, Max:%d, Min:%d, Mean:%d, Median:%d' \
% (wordSum, np.max(wordNum), np.min(wordNum), np.mean(wordNum), np.median(wordNum))
