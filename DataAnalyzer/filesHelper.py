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
    
    # (char, absX, absY, absX-startX, absY-startY, startX, startY, letterPosX[charNo], letterPosY[charNo]))
    char = [pPos[0] for pPos in pointPos]
    absX = [pPos[1] for pPos in pointPos]
    absY = [pPos[2] for pPos in pointPos]
    userName = [pPos[3].split('_')[0] for pPos in pointPos]
    validMarkQ = np.ones(len(char), int)

    totalArray = np.array(zip(char, userName, absX, absY, validMarkQ), 
        dtype=[('char', 'S5'), ('userName', 'S5'), ('absX', float), ('absY', float), ('validMarkQ', int)])
    totalArray.sort(order='char')

    char = totalArray['char']
    absX = totalArray['absX']
    absY = totalArray['absY']
    lastChar = 0

    for i in range(len(char)):
        if (i < len(char)-1 and char[i] != char[i+1]) or i == len(char)-1:
            # Check range: [lastChar, i]
            if i - lastChar > 10 and char[i] != '-':
                xQ1 = np.percentile(absX[lastChar:i+1], 25)
                xQ3 = np.percentile(absX[lastChar:i+1], 75)
                xIQR = xQ3 - xQ1
                xLower = xQ1 - 1.5 * xIQR
                xUpper = xQ3 + 1.5 * xIQR

                yQ1 = np.percentile(absY[lastChar:i+1], 25)
                yQ3 = np.percentile(absY[lastChar:i+1], 75)
                yIQR = yQ3 - yQ1
                yLower = yQ1 - 1.5 * yIQR
                yUpper = yQ3 + 1.5 * yIQR

                for j in range(lastChar, i+1):
                    if absX[j] < xLower or absX[j] > xUpper or absY[j] < yLower or absY[j] > yUpper:
                        totalArray['validMarkQ'][j] = 0

            lastChar = i + 1

    writeFile = open(fileName, 'w')
    writeFile.write('char, userName, absX, absY, validMarkQ\n')
    for array in totalArray:
        writeFile.write(str(array).strip('()') + '\n')
    # writeFile.write('targetChar,absoluteX,absoluteY,relativeX,relativeY,leftUpX,leftUpY,standardX,standardY\n')
    # # Space is replaced by '-'
    # # pointPos[i] = (character, absoluteX/Y, relativeX/Y, left-up-X/Y, standardX/Y)
    # for pointInfo in pointPos:
    #     writeFile.write(str(pointInfo).strip('()') + '\n')

def savePointPairResults(fileName, pointPair):
    "Save the coordinates of the point pair vectors"

    if True:
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

        validMarkQ = np.ones(len(charPair), int)

        totalArray = np.array(zip(charPair, pattern, userName, vecX, vecY, vecLen, rad1, rad2,
            KbdVecX, KbdVecY, KbdVecLen, KbdRad1, KbdRad2, 
            RatioVecX, RatioVecY, RatioVecLen, DistRad, validMarkQ), 
            dtype=[('charPair', 'S5'), ('pattern', 'S5'), ('userName', 'S15'), ('vecX', float), ('vecY', float), ('vecLen', float), ('rad1', float), ('rad2', float), 
            ('KbdVecX', float), ('KbdVecY', float), ('KbdVecLen', float), ('KbdRad1', float), ('KbdRad2', float), 
            ('RatioVecX', float), ('RatioVecY', float), ('RatioVecLen', float), ('DistRad', float), ('validMarkQ', int)])
        totalArray.sort(order='charPair')

        # Mark the invalid points
        lastChar = 0
        charPair = totalArray['charPair']
        vecX = totalArray['vecX']
        vecY = totalArray['vecY']
        vecLen = totalArray['vecLen']
        for i in range(len(charPair)):
            if (i < len(charPair)-1 and charPair[i] != charPair[i+1]) or i == len(charPair)-1:
                # Check range: [lastChar, i]
                if i - lastChar > 10:
                    xQ1 = np.percentile(vecX[lastChar:i+1], 25)
                    xQ3 = np.percentile(vecX[lastChar:i+1], 75)
                    xIQR = xQ3 - xQ1
                    xLower = xQ1 - 1.5 * xIQR
                    xUpper = xQ3 + 1.5 * xIQR

                    yQ1 = np.percentile(vecY[lastChar:i+1], 25)
                    yQ3 = np.percentile(vecY[lastChar:i+1], 75)
                    yIQR = yQ3 - yQ1
                    yLower = yQ1 - 1.5 * yIQR
                    yUpper = yQ3 + 1.5 * yIQR
                    
                    lenQ1 = np.percentile(vecLen[lastChar:i+1], 25)
                    lenQ3 = np.percentile(vecLen[lastChar:i+1], 75)
                    lenIQR = lenQ3 - lenQ1
                    lenLower = lenQ1 - 1.5 * lenIQR
                    lenUpper = lenQ3 + 1.5 * lenIQR

                    for j in range(lastChar, i+1):
                        if vecX[j] < xLower or vecX[j] > xUpper or vecY[j] < yLower or vecY[j] > yUpper or vecLen[j] < lenLower or vecLen[j] > lenUpper:
                            totalArray['validMarkQ'][j] = 0

                lastChar = i + 1

        # textStructArray = np.array(textStruct, dtype = [('text', 'S50'), ('totalValue', float), ('oneHandValue', float)])
        # print len(pointPair), len(charPair), charPair[3]
        
    writeFile = open(fileName, 'w')
    writeFile.write('charPair, pattern, userName, vecX, vecY, vecLen, rad(-pi~pi), rad(0~2pi), KbdVecX, KbdVecY, KbdVecLen, KbdRad1, KbdRad2, RatioVecX, RatioVecY, RatioVecLen, DistRad, validMarkQ\n')
    for array in totalArray:
        writeFile.write(str(array).strip('()') + '\n')

    # (characterPair, pattern, vectorX, vectorY)
    # for pairInfo in pointPair:
    # for i in range(len(pointPair)):
    #     writeFile.write(str(pointPair[i]).strip('()') + 
    #         ',%f,%f,%f\n' % (vecLen[i], rad1[i], rad2[i]))
