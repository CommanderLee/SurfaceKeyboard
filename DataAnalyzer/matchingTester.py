# Test different algorithms for word matching.
# Zhen Li, Tsinghua University.
import csv
import matplotlib.pyplot as plt
from pylab import *
import Tkinter, tkFileDialog
import numpy as np 
import math
import pickle

from constants import *
from keyboardLayoutHelper import *
from filesHelper import *

# Main

# Parse the corpus
allWords = loadCorpus()
wordDic = {}
wordCnt = {}

if False:
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

    corpusFile = file('corpus.pkl', 'wb')
    pickle.dump(wordDic, corpusFile, True)
    pickle.dump(wordCnt, corpusFile, True)
else:
    corpusFile = file('corpus.pkl', 'rb')
    wordDic = pickle.load(corpusFile)
    wordCnt = pickle.load(corpusFile)

# print wordDic
# print wordCnt

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
resultStr = ''
if openFiles:
    results = []
    # code -> number. total word number and error number.
    # So the correct(and almost) number = total word number - error number
    totalWordPattern = {}
    totalErrorPattern = {}

    # (word, pos, canLen)
    totalWordPos = []
    
    # pointPos[i] = (character, absoluteX/Y, relativeX/Y, left-up-X/Y, standardX/Y)
    totalPointPos = []

    # (characterPair, pattern, vectorX, vectorY)
    totalPointPair = []
    
    for dataFile in openFiles:
        # Load testing sentences
        textFileName = dataFile.name[0:len(dataFile.name)-9] + 'TaskText.txt'
        texts = loadTestingSet(textFileName)

        fileNameAbbr = dataFile.name.split('/')[-1][15:25]
        resultStr += '-' + fileNameAbbr
        print '...' + fileNameAbbr + '... :'

        # X, Y, Time, TaskIndex_PointIndex_FingerId, PointType
        # Hint: '-' will be removed by the function numpy.genfromtxt 
        # (ref: http://docs.scipy.org/doc/numpy/user/basics.io.genfromtxt.html)
        data = np.genfromtxt(dataFile.name, dtype = None, delimiter = ',', names = True)

        # Load data column
        dataX = data['X']
        dataY = data['Y']
        dataTime = data['Time']
        dataId = [_id.strip() for _id in data['TaskIndex_PointIndex_FingerId']]
        dataType = [_type.strip() for _type in data['PointType']]

        textNo = int(dataId[0].split('_')[0])
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
                idList = dataId[dataNo].split('_')
                # print idList
                taskNo = int(idList[0])
                # Go to next text
                if taskNo > textNo:
                    textNo = taskNo - 1
                    break

                # pointNo = idList[1]
                # fingerId = idList[2]
                if dataType[dataNo] == 'Touch' or dataType[dataNo] == 'Recover':
                    listX.append(dataX[dataNo])
                    listY.append(dataY[dataNo])

                dataNo += 1
                
            if len(currText) != len(listX):    
                print "Warning: %d letters, %d touch points." % (len(currText), len(listX))

            listNo = 0
            for word in currText.split(' '):
                wordLen = len(word)
                
                for i in range(0, wordLen):
                    # Save single points
                    char = word[i]
                    charNo = ord(char) - ord('a')
                    absX = listX[i + listNo]
                    absY = listY[i + listNo]
                    totalPointPos.append((char, absX, absY, absX-startX, absY-startY, startX, startY, letterPosX[charNo], letterPosY[charNo]))
                    
                    # Save point pairs
                    if i > 0:
                        # point pair <word[i-1], word[i]>
                        charPair = word[i-1:i+1]
                        pattern = handCode[ord(word[i-1]) - ord('a') + 1] + handCode[ord(word[i]) - ord('a') + 1]
                        vecX = listX[i + listNo] - listX[i - 1 + listNo]
                        vecY = listY[i + listNo] - listY[i - 1 + listNo]
                        totalPointPair.append((charPair, pattern, vecX, vecY))
                    elif listNo > 0:
                        # point pair <space, word[0]>
                        charPair = '-' + word[i]
                        pattern = '2' + handCode[ord(word[i]) - ord('a') + 1]
                        vecX = listX[i + listNo] - listX[i - 1 + listNo]
                        vecY = listY[i + listNo] - listY[i - 1 + listNo]
                        totalPointPair.append((charPair, pattern, vecX, vecY))

                    if i == wordLen - 1 and i + 1 + listNo < len(listX):
                        # point pair <word[len-1], space>
                        charPair = word[i] + '-'
                        pattern = handCode[ord(word[i]) - ord('a') + 1] + '2'
                        vecX = listX[i + 1 + listNo] - listX[i + listNo]
                        vecY = listY[i + 1 + listNo] - listY[i + listNo]
                        totalPointPair.append((charPair, pattern, vecX, vecY))

                        # Save spacebar(replaced by '-') point information
                        absX = listX[wordLen + listNo]
                        absY = listY[wordLen + listNo]
                        totalPointPos.append(('-', absX, absY, absX-startX, absY-startY, startX, startY, -1, -1))
                    

                # Calculate user codes
                userCodes = calcUserCodes(listX[listNo:listNo+wordLen], midX, rangeX)
                
                # Save the correct code
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
                        totalWordPos.append((word, 1, len(wordProbArray)))
                    else:
                        # Find the word position
                        for i in range(1, len(wordProbArray)):
                            if word == wordProbArray[i][0]:
                                totalWordPos.append((word, i, len(wordProbArray)))
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
                    totalWordPos.append((word, -1, -1))

                    if {code}.issubset(errorPattern.keys()):
                        errorPattern[code] += 1
                    else:
                        errorPattern[code] = 1

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
        result += 'Error Word Pattern: %r \n' % (errorPatternList)
        print result
        results.append(result)

        # Save to file
        saveErrorPatternResults('%s_result.csv' % (dataFile.name), errorPattern, wordPattern, wordDic)
        
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

    saveErrorPatternResults('Result/matchingResult%s.csv' % (resultStr), totalErrorPattern, totalWordPattern, wordDic)
    
    saveWordPositionResults('Result/wordPosResult%s.csv' % (resultStr), totalWordPos, wordDic)

    saveSinglePointResults('Result/pointPosResult%s.csv' % (resultStr), totalPointPos)

    savePointPairResults('Result/pointPairResult%s.csv' % (resultStr), totalPointPair)

    # fileNo = 8

    # saveErrorPatternResults('matchingResult%d.csv' % (fileNo), totalErrorPattern, totalWordPattern, wordDic)
    
    # saveWordPositionResults('wordPosResult%d.csv' % (fileNo), totalWordPos, wordDic)

    # saveSinglePointResults('pointPosResult%d.csv' % (fileNo), totalPointPos)

    # savePointPairResults('pointPairResult%d.csv' % (fileNo), totalPointPair)