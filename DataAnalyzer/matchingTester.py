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
texts = [text.strip().lower() for text in textFile]
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
    for word in sentence.split(' '):
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
        midX = (np.max(dataX) + np.min(dataX)) / 2
        rangeX = np.max(dataX) - np.min(dataX)
        print midX, rangeX

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
                for i in range(listNo, listNo + wordLen):
                    if listX[i] < midX:
                        userCode += '0'
                    else:
                        userCode += '1'

                if {userCode}.issubset(wordDic.keys()):
                    selWords = wordDic[userCode]
                    # Get user's vecL, vecR (vector within left/right hand)


                    # Get vector of possible word.


                    # Compare each of the possible word.


                    # TODO: if correct
                    # if xxx == word:
                    if {word}.issubset(selWords):
                        correctNum += 1
                    else:
                        wrongNum += 1
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
