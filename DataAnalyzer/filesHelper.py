# Some functions to load the data and save the result
# Zhen Li, Tsinghua University.

import string
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

def savePointPairResults(fileName, pointPair):
    "Save the coordinates of the point pair vectors"
    writeFile = open(fileName, 'w')
    writeFile.write('charPair, pattern, vecX, vecY\n')
    # (characterPair, pattern, vectorX, vectorY)
    for pairInfo in pointPair:
        writeFile.write(str(pairInfo).strip('()') + '\n')
