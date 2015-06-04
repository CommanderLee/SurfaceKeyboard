# Some functions to load the data and save the result
# Zhen Li, Tsinghua University.

import string
import numpy as np
import math

from constants import *
from keyboardLayoutHelper import encode

def loadCorpus(corpusNum):
    "Load words as corpus"
    words = []

    if True:
        # MacKenzie
        textFile = open('Data/TaskText.txt', 'r')
        sentences = [text.strip().lower().split(' ') for text in textFile]
        for sentence in sentences:
            words += sentence
    if True:
        # en_US_wordlist from Yi, Xin.
        textFile = open('en_US_wordlist.combined', 'r')
        rawData = [text.strip().split(',') for text in textFile]
        if corpusNum > len(rawData) or corpusNum < 0:
            corpusNum = len(rawData)
        for data in rawData[1:corpusNum]:
            word = ''
            for char in data[0].split('=')[1].lower():
                if {char}.issubset(string.letters):
                    word += char
            words.append(word)
    # print words
    print '%d words.' % (len(words))
    return words

def loadTestingSet(fileName = 'Data/TaskText.txt'):
    "Load testing set"
    textFile = open(fileName, 'r')
    texts = [text.strip().lower() for text in textFile]
    return texts

def countLeftNum(code):
    "Count the number of left points"
    leftNum = 0
    for char in code:
        if char == '0':
            leftNum += 1
    return leftNum

def saveErrorPatternResults(fileName, errorPattern, wordPattern, wordDic):
    "Save the error pattern to .csv file"
    writeFile = open(fileName, 'w')
    writeFile.write('code,leftNum,rightNum,codeLen,errRate,errNum,wordNum,wordTotalNum\n')
    for (code, wordNum) in wordPattern.items():
        codeLen = len(code)
        leftNum = countLeftNum(code)
        rightNum = codeLen - leftNum
        
        errNum = 0
        if {code}.issubset(errorPattern):
            errNum = errorPattern[code]
        errRate = float(errNum) / wordNum

        writeFile.write('#%s,%d,%d,%d,%f,%d,%d,%d\n' % (code, leftNum, rightNum, codeLen, errRate, errNum, wordNum, len(wordDic[code])))

def saveWordPositionResults(fileName, wordPos, wordDic):
    "Save the actual word position in the candidates list"
    writeFile = open(fileName, 'w')
    writeFile.write('word,code,leftNum,rightNum,codeLen,wordPos,candidateLen,wordTotalNum\n')
    # wordPos[i] = (word, position, candidateListLen)
    for (word, pos, canLen) in wordPos:
        code = encode(word)
        codeLen = len(code)
        leftNum = countLeftNum(code)
        rightNum = codeLen - leftNum
        writeFile.write('%s,#%s,%d,%d,%d,%d,%d,%d\n' % (word, code, leftNum, rightNum, codeLen, pos, canLen, len(wordDic[code])))

def saveSinglePointResults(fileName, pointPos):
    "Save the position of single points"
    writeFile = open(fileName, 'w')
    writeFile.write('targetChar,absoluteX,absoluteY,relativeX,relativeY,leftUpX,leftUpY,standardX,standardY\n')
    # Space is replaced by '-'
    # pointPos[i] = (character, absoluteX/Y, relativeX/Y, left-up-X/Y, standardX/Y)
    for pointInfo in pointPos:
        writeFile.write(str(pointInfo).strip('()') + '\n')

def savePointPairResults(fileName, pointPair, analyze):
    "Save the coordinates of the point pair vectors"

    if analyze:
        charPair = [pPair[0] for pPair in pointPair]
        pattern = [pPair[1] for pPair in pointPair]
        vecX = np.array([pPair[2] for pPair in pointPair])
        vecY = np.array([pPair[3] for pPair in pointPair])
        userName = [pPair[4] for pPair in pointPair]
        
        vecLen = np.sqrt(np.power(vecX, 2) + np.power(vecY, 2))

        # rad1: -pi ~ pi. rad2: 0 ~ 2pi
        rad1 = np.zeros(len(charPair))
        rad2 = np.zeros(len(charPair))
        for i in range(len(charPair)):
            if vecLen[i] != 0:
                if vecY[i] > 0:
                    rad1[i] = math.acos(vecX[i] / vecLen[i])
                    rad2[i] = rad1[i]
                else:
                    rad1[i] = -math.acos(vecX[i] / vecLen[i])
                    rad2[i] = math.pi + math.acos(-vecX[i] / vecLen[i])
        # rad1 = np.arccos(vecX / vecLen) * (vecY >= 0) + (-np.arccos(vecX / vecLen)) * (vecY < 0)
        # rad2 = np.arccos(vecX / vecLen) * (vecY >= 0) + (np.pi + np.arccos(-vecX / vecLen)) * (vecY < 0)

        KbdVecX = np.zeros(len(charPair))
        KbdVecY = np.zeros(len(charPair))
        KbdVecLen = np.zeros(len(charPair))
        KbdRad1 = np.zeros(len(charPair))
        KbdRad2 = np.zeros(len(charPair))

        RatioVecX = np.zeros(len(charPair))
        RatioVecY = np.zeros(len(charPair))
        RatioVecLen = np.zeros(len(charPair))
        DistRad = np.zeros(len(charPair))
        
        for i in range(len(charPair)):
            chPair = charPair[i]
            if chPair[0] != '-' and chPair[1] != '-':
                ord1 = ord(chPair[0]) - ord('a')
                ord2 = ord(chPair[1]) - ord('a')
                
                KbdVecX[i] = letterPosX[ord2] - letterPosX[ord1]
                KbdVecY[i] = letterPosY[ord2] - letterPosY[ord1]
                KbdVecLen[i] = math.sqrt(math.pow(KbdVecX[i], 2) + math.pow(KbdVecY[i], 2))

                if KbdVecLen[i] != 0:
                    if KbdVecY[i] > 0:
                        KbdRad1[i] = math.acos(KbdVecX[i] / KbdVecLen[i])
                        KbdRad2[i] = KbdRad1[i]
                    else:
                        KbdRad1[i] = -math.acos(KbdVecX[i] / KbdVecLen[i])
                        KbdRad2[i] = math.pi + math.acos(- KbdVecX[i] / KbdVecLen[i])

                    if KbdVecX[i] != 0:
                        RatioVecX[i] = vecX[i] / KbdVecX[i]
                    else:
                        RatioVecX[i] = vecX[i]
                    if KbdVecY[i] != 0:
                        RatioVecY[i] = vecY[i] / KbdVecY[i]
                    else:
                        RatioVecY[i] = vecY[i]

                    RatioVecLen[i] = vecLen[i] / (KbdVecLen[i] + 1)
                    DistRad[i] = min(abs(rad1[i] - KbdRad1[i]), abs(rad2[i] - KbdRad2[i]))

        totalArray = np.array(zip(charPair, pattern, userName, vecX, vecY, vecLen, rad1, rad2,
            KbdVecX, KbdVecY, KbdVecLen, KbdRad1, KbdRad2, 
            RatioVecX, RatioVecY, RatioVecLen, DistRad), 
            dtype=[('charPair', 'S5'), ('pattern', 'S5'), ('userName', 'S15'), ('vecX', float), ('vecY', float), ('vecLen', float), ('rad1', float), ('rad2', float), 
            ('KbdVecX', float), ('KbdVecY', float), ('KbdVecLen', float), ('KbdRad1', float), ('KbdRad2', float), 
            ('RatioVecX', float), ('RatioVecY', float), ('RatioVecLen', float), ('DistRad', float)])
        totalArray.sort(order='charPair')


        # textStructArray = np.array(textStruct, dtype = [('text', 'S50'), ('totalValue', float), ('oneHandValue', float)])
        # print len(pointPair), len(charPair), charPair[3]
        
    writeFile = open(fileName, 'w')
    writeFile.write('charPair, pattern, userName, vecX, vecY, vecLen, rad(-pi~pi), rad(0~2pi), KbdVecX, KbdVecY, KbdVecLen, KbdRad1, KbdRad2, RatioVecX, RatioVecY, RatioVecLen, DistRad\n')
    for array in totalArray:
        writeFile.write(str(array).strip('()') + '\n')

    # (characterPair, pattern, vectorX, vectorY)
    # for pairInfo in pointPair:
    # for i in range(len(pointPair)):
    #     writeFile.write(str(pointPair[i]).strip('()') + 
    #         ',%f,%f,%f\n' % (vecLen[i], rad1[i], rad2[i]))
