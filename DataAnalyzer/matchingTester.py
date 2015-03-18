# Test different algorithms for word matching.
# Zhen Li, Tsinghua University.
import csv
import matplotlib.pyplot as plt
from pylab import *
import Tkinter, tkFileDialog
import numpy as np 
import math

from constants import *
from keyboardLayoutHelper import *

def encode(string):
    "Encode the word using handCode rules."
    code = ''
    for char in string:
        code += handCode[ord(char) - ord('a')]
    return code

def loadCorpus():
    "Load words as corpus"
    # MacKenzie
    textFile = open('TaskText.txt', 'r')
    sentences = [text.strip().lower().split(' ') for text in textFile]
    words = []
    for sentence in sentences:
        words += sentence
    # print words
    print '%d words.' % (len(words))
    return words

# Main

# Parse the corpus
allWords = loadCorpus()
wordDic = {}
wordCnt = {}
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

print wordDic
print wordCnt

# Load testing sentences
textFile = open('TaskText.txt', 'r')
texts = [text.strip().lower() for text in textFile]

# Number of words sharing the same code
sameCodeCnt = [len(wordSet) for wordSet in wordDic.values()]
print 'Number of words sharing the same code:\n    Max:%d, Mean:%d, Median:%d' \
% (np.max(sameCodeCnt), np.mean(sameCodeCnt), np.median(sameCodeCnt))

# Frequency of words
wordNum = wordCnt.values()
wordSum = np.sum(wordNum)
print 'Word Frequency:\n    Sum:%d, Max:%d, Min:%d, Mean:%d, Median:%d' \
% (wordSum, np.max(wordNum), np.min(wordNum), np.mean(wordNum), np.median(wordNum))

# Load user data files
tkObj = Tkinter.Tk()
tkObj.file_opt = options = {}
options['defaultextension'] = '.csv'

openFiles = tkFileDialog.askopenfiles('r')
if openFiles:
    for dataFile in openFiles:
        print dataFile.name + ':'

        # X, Y, Time, TaskNo-PointNo-FingerId, PointType
        # Hint: '-' will be removed by the function numpy.genfromtxt 
        # (ref: http://docs.scipy.org/doc/numpy/user/basics.io.genfromtxt.html)
        data = np.genfromtxt(dataFile.name, dtype = None, delimiter = ',', names = True)

        # Load data column
        dataX = data['X']
        dataY = data['Y']
        dataTime = data['Time']
        dataId = [_id.strip() for _id in data['TaskNoPointNoFingerId']]
        dataType = [_type.strip() for _type in data['PointType']]

        textNo = int(dataId[0].split('-')[0])
        textLen = len(texts)
        dataNo = 0
        dataLen = len(data)

        # TODO: Get a probability function for judging Left / Right
        startX = np.min(dataX)
        startY = np.min(dataY)
        midX = (np.max(dataX) + np.min(dataX)) / 2
        # midY = (np.max(dataY) + np.min(dataY)) / 2
        rangeX = np.max(dataX) - startX
        rangeY = np.max(dataY) - startY
        print midX, rangeX, rangeY

        correctNum = 0
        wrongNum = 0
        emptyNum = 0 

        while textNo < textLen:
            # Parse every text (sentence)
            currText = texts[textNo]
            # print "%d: %s" % (textNo, currText)
            
            # Get point list of current sentence
            listX = []
            listY = []

            # Get all 'Touch' point and ignore 'Move' point
            while dataNo < dataLen:
                idList = dataId[dataNo].split('-')
                # print idList
                taskNo = int(idList[0])
                # Go to next text
                if taskNo > textNo:
                    break

                # pointNo = idList[1]
                # fingerId = idList[2]
                if dataType[dataNo] == 'Touch':
                    listX.append(dataX[dataNo])
                    listY.append(dataY[dataNo])

                dataNo += 1
                
            if len(currText) != len(listX):    
                print "Warning: %d letters, %d touch points." % (len(currText), len(listX))

            listNo = 0
            for word in currText.split(' '):
                wordLen = len(word)
                userCode = ''

                # Get user's vecL, vecR (vector within left/right hand)
                myPntIdL, myPntIdR = [], []
                myVecL, myVecR = [], []

                for i in range(listNo, listNo + wordLen):
                    if listX[i] < midX:
                        userCode += '0'
                        if len(myPntIdL) > 0:
                            myVecL.append((listX[i] - listX[myPntIdL[-1]], listY[i] - listY[myPntIdL[-1]]))
                        myPntIdL.append(i)
                    else:
                        userCode += '1'
                        if len(myPntIdR) > 0:
                            myVecR.append((listX[i] - listX[myPntIdR[-1]], listY[i] - listY[myPntIdR[-1]]))
                        myPntIdR.append(i)
                myPntL = [(listX[i], listY[i]) for i in myPntIdL]
                myPntR = [(listX[i], listY[i]) for i in myPntIdR]
                # print myVecL, myVecR

                if {userCode}.issubset(wordDic.keys()):
                    selWords = wordDic[userCode]
                    wordProb = []
                    # Test each candidates
                    for candWord in selWords:
                        # Get vector of possible word.
                        [pntL, pntR, vecL, vecR] = calcWordVec(candWord)
                        distance = 0
                        if len(myVecL) > 0:
                            sub = np.array(myVecL) - np.array(vecL)
                            subLen = [math.sqrt(si.dot(si)) for si in sub]
                            distance += np.sum(subLen)
                        elif len(myPntL) > 0:
                            sub = []
                            if len(myPntR) > 0:
                                sub = (np.array(myPntR[0]) - np.array(myPntL[0])) - (np.array(pntR[0]) - np.array(pntL[0]))
                            else: 
                                sub = np.array(myPntL[0]) - np.array(pntL[0])
                            distance += math.sqrt(sub.dot(sub))

                        if len(myVecR) > 0:
                            sub = np.array(myVecR) - np.array(vecR)
                            subLen = [math.sqrt(si.dot(si)) for si in sub]
                            distance += np.sum(subLen)
                        elif len(myPntR) > 0:
                            sub = []
                            if len(myPntL) > 0:
                                sub = (np.array(myPntR[0]) - np.array(myPntL[0])) - (np.array(pntR[0]) - np.array(pntL[0]))
                            else: 
                                sub = np.array(myPntR[0]) - np.array(pntR[0])
                            distance += math.sqrt(sub.dot(sub))

                        # Compare each of the possible word.
                        # Try: if vecList=[], (only one point), calculate the probability of that point
                        wordProb.append((candWord, distance))

                    wordProbArray = np.array(wordProb, dtype = [('word', 'S20'), ('dist', int)])
                    wordProbArray.sort(order = 'dist')
                    # print wordProbArray

                    # TODO: if correct
                    if wordProbArray[0][0] == word:
                        correctNum += 1
                    else:
                        wrongNum += 1
                        print 'Wrong: ' + word
                        print wordProbArray
                else:
                    emptyNum += 1
                # Jump: word length + ' '
                listNo += wordLen + 1

            # Break when finish processing the data set
            if dataNo >= dataLen:
                break

            textNo += 1

        print 'Correct:%d, Wrong:%d, Empty:%d' % (correctNum, wrongNum, emptyNum)
        percFact = 100 / float(correctNum + wrongNum + emptyNum)
        print '(%%) Correct:%f%%, Wrong:%f%%, Empty:%f%%' % (correctNum * percFact, wrongNum * percFact, emptyNum * percFact)
