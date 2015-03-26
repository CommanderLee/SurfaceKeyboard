# Test different algorithms for word matching.
# Zhen Li, Tsinghua University.
import csv
import matplotlib.pyplot as plt
from pylab import *
import Tkinter, tkFileDialog
import numpy as np 
import math
import string

from constants import *
from keyboardLayoutHelper import *

def encode(word):
    "Encode the word using handCode rules."
    code = ''
    for char in word:
        code += handCode[ord(char) - ord('a')]
    return code

def countLeftNum(code):
    "Count the number of left points"
    leftNum = 0
    for char in code:
        if char == '0':
            leftNum += 1
    return leftNum

def saveResults(fileName, outputList, errorPattern, wordPattern, wordDic):
    "Save the error pattern to .csv file"
    writeFile = open(fileName, 'w')
    writeFile.write('code,leftNum,rightNum,codeLen,errRate,errNum,wordNum,wordTotalNum\n')
    for errorData in outputList:
        code = errorData[0]
        errRate = -errorData[1]
        errNum = errorPattern[code]
        wordNum = wordPattern[code]
        leftNum = countLeftNum(code)
        rightNum = len(code) - leftNum
        writeFile.write('#%s,%d,%d,%d,%f,%d,%d,%d\n' % (code, leftNum, rightNum, len(code), errRate, errNum, wordNum, len(wordDic[code])))


def loadCorpus():
    "Load words as corpus"
    words = []

    if True:
        # MacKenzie
        textFile = open('TaskText.txt', 'r')
        sentences = [text.strip().lower().split(' ') for text in textFile]
        for sentence in sentences:
            words += sentence
    if True:
        # en_US_wordlist from Yi, Xin.
        textFile = open('en_US_wordlist.combined', 'r')
        rawData = [text.strip().split(',') for text in textFile]
        for data in rawData[1:20000]:
            word = ''
            for char in data[0].split('=')[1].lower():
                if {char}.issubset(string.letters):
                    word += char
            words.append(word)
    # print words
    print '%d words.' % (len(words))
    return words

def calcUserCodes(pntListX, midX, rangeX):
    "Calculate the user codes using recursion"
    codes = []
    if len(pntListX) == 1:
        suffixCodes = ['']
    else:
        suffixCodes = calcUserCodes(pntListX[1:], midX, rangeX)
    # Using a experimental constant. TODO: Using a probability model
    relativePos = (pntListX[0] - midX) / rangeX
    if abs(relativePos) < 0.05:
        for suffix in suffixCodes:
            codes.append('0' + suffix)
            codes.append('1' + suffix)
    elif pntListX[0] < midX:
        for suffix in suffixCodes:
            codes.append('0' + suffix)
    else:
        for suffix in suffixCodes:
            codes.append('1' + suffix)
    return codes

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

# print wordDic
# print wordCnt

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
    results = []
    # code -> number
    totalWordPattern = {}
    totalErrorPattern = {}
    for dataFile in openFiles:
        print '...' + dataFile.name[-30:-4] + ':'

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

        rangeX = np.max(dataX) - startX
        rangeY = np.max(dataY) - startY

        midX = (np.max(dataX) + np.min(dataX)) / 2 + rangeX * 0.1
        # midY = (np.max(dataY) + np.min(dataY)) / 2

        print 'start:(%f, %f), mid:(%f, xx), range:%f x %f' % (startX, startY, midX, rangeX, rangeY)

        correctNum = 0
        almostCorrNum = 0
        wordErrNum = 0
        codeErrNum = 0

        # Analyze the reason of error
        errorWordPos = []
        # code -> number
        wordPattern  = {}
        errorPattern = {}

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
                userCodes = calcUserCodes(listX[listNo:listNo+wordLen], midX, rangeX)
                code = encode(word)
                if {code}.issubset(wordPattern):
                    wordPattern[code] += 1
                else:
                    wordPattern[code] = 1

                # Don't need to try if the real code is not in the possible set
                if {code}.issubset(userCodes):
                    wordProb = []
                    # Try each userCode
                    for userCode in userCodes:
                        if {userCode}.issubset(wordDic.keys()):
                            # Get user's vecL, vecR (vector within left/right hand)
                            myPntIdL, myPntIdR = [], []
                            myVecL, myVecR = [], []
                            for i in range(wordLen):
                                if userCode[i] == '0':
                                    if len(myPntIdL) > 0:
                                        myVecL.append((listX[listNo + i] - listX[myPntIdL[-1]], listY[listNo + i] - listY[myPntIdL[-1]]))
                                    myPntIdL.append(listNo + i)
                                else:
                                    if len(myPntIdR) > 0:
                                        myVecR.append((listX[listNo + i] - listX[myPntIdR[-1]], listY[listNo + i] - listY[myPntIdR[-1]]))
                                    myPntIdR.append(listNo + i)

                            myPntL = [(listX[i], listY[i]) for i in myPntIdL]
                            myPntR = [(listX[i], listY[i]) for i in myPntIdR]
                            # print myVecL, myVecR

                            selWords = wordDic[userCode]
                        
                            # Test each candidates
                            for candWord in selWords:
                                # Get vector of possible word.
                                [pntL, pntR, vecL, vecR] = calcWordVec(candWord)
                                distance = 0
                                if len(myVecL) > 0:
                                    sub = np.array(myVecL) - np.array(vecL)
                                    subLen = [math.sqrt(si.dot(si)) for si in sub]
                                    distance += np.sum(subLen)
                                    # print '    %s  (1)  %f' % (candWord, np.sum(subLen))
                                elif len(myPntL) > 0:
                                    # Only one point on the Left
                                    # sub = []
                                    if len(myPntR) > 0:
                                        # More than one point on the Right
                                        sub = (np.array(myPntR[0]) - np.array(myPntL[0])) - (np.array(pntR[0]) - np.array(pntL[0]))
                                        # If word is long enough, then no need to add this left point distance. That is, len -> inf, Dist -> 0.
                                        distance += math.sqrt(sub.dot(sub)) * math.exp(-wordLen / 5.0)
                                    # else: 
                                    # TODO: Use a better model, instead of a intuitive one.
                                    relativeMyPnt = np.array(myPntL[0]) - np.array((startX, startY))
                                    relativePnt = np.array((pntL[0][0]/445*rangeX, pntL[0][1]/188*rangeY))
                                    sub = relativeMyPnt - relativePnt
                                    # if candWord == word:
                                    #     print 'Correct', math.sqrt(sub.dot(sub)), myPntL[0], relativeMyPnt, pntL[0], relativePnt
                                    # else:
                                    #     print 'Wrong', math.sqrt(sub.dot(sub)), myPntL[0], relativeMyPnt, pntL[0], relativePnt
                                    distance += math.sqrt(sub.dot(sub)) * math.exp(-wordLen / 10.0)
                                
                                if len(myVecR) > 0:
                                    sub = np.array(myVecR) - np.array(vecR)
                                    subLen = [math.sqrt(si.dot(si)) for si in sub]
                                    distance += np.sum(subLen)
                                    # print '    %s  (3)  %f' % (candWord, np.sum(subLen))
                                elif len(myPntR) > 0:
                                    # Only one point on the Right
                                    # sub = []
                                    if len(myPntL) > 0:
                                        # More than one point on the Left
                                        sub = (np.array(myPntR[0]) - np.array(myPntL[0])) - (np.array(pntR[0]) - np.array(pntL[0]))
                                        # If word is long enough, then no need to add this right point distance. That is, len -> inf, Dist -> 0.
                                        distance += math.sqrt(sub.dot(sub)) * math.exp(-wordLen / 5.0)
                                    # else: 
                                    relativeMyPnt = np.array(myPntR[0]) - np.array((startX, startY))
                                    relativePnt = np.array((pntR[0][0]/445*rangeX, pntR[0][1]/188*rangeY))
                                    sub = relativeMyPnt - relativePnt
                                    distance += math.sqrt(sub.dot(sub)) * math.exp(-wordLen / 10.0)
                                
                                # Compare each of the possible word.
                                # Try: if vecList=[], (only one point), calculate the probability of that point
                                wordProb.append((candWord, distance))

                    # At least one possible answer.
                    assert len(wordProb) > 0

                    wordProbArray = np.array(wordProb, dtype = [('word', 'S20'), ('dist', int)])
                    wordProbArray.sort(order = 'dist')
                    # print wordProbArray

                    # Correct
                    if wordProbArray[0][0] == word:
                        correctNum += 1
                    else:
                        # Find the word position
                        for i in range(1, len(wordProbArray)):
                            if word == wordProbArray[i][0]:
                                # Almost correct
                                if i <= 2:
                                    almostCorrNum += 1

                                    print 'Almost Correct: ' + word
                                    print '    %r' % (wordProbArray.tolist()[:3])
                                # Error
                                else:
                                    wordErrNum += 1
                                    errorWordPos.append(i)

                                    if {code}.issubset(errorPattern.keys()):
                                        errorPattern[code] += 1
                                    else:
                                        errorPattern[code] = 1

                                    print 'Word Error: ' + word
                                    print '    %r' % (wordProbArray.tolist())  
                                break
                else:
                    # Code error
                    codeErrNum += 1
                    print 'Code Error. word:%s, correct:%s, user:' % (word, code)
                    print userCodes

                # Jump: word length + SPACE
                listNo += wordLen + 1

            # Break when finish processing the data set
            if dataNo >= dataLen:
                break

            textNo += 1

        result = 'Correct:%d, Almost Correct:%d, Word Error:%d, Code Error:%d \n' % (correctNum, almostCorrNum, wordErrNum, codeErrNum)
        
        wordSum = (correctNum + almostCorrNum + wordErrNum + codeErrNum)
        percFact = 100 / float(wordSum)
        result += '(Percentile) Correct:%f%%, Almost Correct:%f%%, Word Error:%f%%, Code Error:%f%% \n' % (correctNum * percFact, almostCorrNum * percFact, wordErrNum * percFact, codeErrNum * percFact)
        
        if len(errorWordPos) > 0:
            result += 'Error Word Position: Min:%d, Max:%d, Mean:%f, Median:%f, Std:%f \n' % (min(errorWordPos), max(errorWordPos), np.mean(errorWordPos), np.median(errorWordPos), np.std(errorWordPos))

        errorPatternList = [(code, -float(errorPattern[code]) / wordPattern[code]) for code in errorPattern.keys()]
        errorPatternArray = np.array(errorPatternList, dtype = [('code', 'S20'), ('errRate', float)])
        errorPatternArray.sort(order = 'errRate')
        result += 'Error Word Pattern: %r \n' % (errorPatternArray.tolist())
        print result
        results.append(result)

        # Save to file
        saveResults('%s_result.csv' % (dataFile.name), errorPatternArray.tolist(), errorPattern, wordPattern, wordDic)
        
        # Add to total dict. (code, number)
        for (c, n) in wordPattern.items():
            if {c}.issubset(totalWordPattern.keys()):
                totalWordPattern[c] += n
            else:
                totalWordPattern[c] = n
        for (c, n) in errorPattern.items():
            if {c}.issubset(totalErrorPattern.keys()):
                totalErrorPattern[c] += n
            else:
                totalErrorPattern[c] = n

    print '------Accuracy Rate------'
    for result in results:
        print result

    totalErrorPatternList = [(code, -float(totalErrorPattern[code]) / totalWordPattern[code]) for code in totalErrorPattern.keys()]
    totalErrorPatternArray = np.array(totalErrorPatternList, dtype = [('code', 'S20'), ('errRate', float)])
    totalErrorPatternArray.sort(order = 'errRate')

    saveResults('matchingResult5.csv', totalErrorPatternArray.tolist(), totalErrorPattern, totalWordPattern, wordDic)
    

