# Check some important features of the testing set.
# Zhen Li, Tsinghua University.

from filesHelper import loadTestingSet
from constants import handCode

def getCharNo(char):
    "Character Mapping-> spacebar:0, a-z:1-26"
    no = 0
    if char != ' ':
        no = ord(char.lower()) - ord('a') + 1

    return no

def getChar(num):
    "Character Mapping-> 0:spacebar, 1-26:a-z"
    char = '-'
    if num != 0:
        char = chr(num - 1 + ord('a'))

    return char

def printMatrixInfo(fileNamePrefix, numMat, listMat):
    "Print the info of the char-pair matrix"
    # Print the total matrix info
    totalMatFileName = fileNamePrefix + '_total.csv'
    writeTotalMat = open(totalMatFileName, 'w')
    tag = [getChar(num) for num in range(27)]
    tagStr = ',' + ','.join(tag)
    writeTotalMat.write(tagStr + '\n')
    for row in range(27):
        rowStr = getChar(row)
        for col in range(27):
            rowStr += ',' + str(numMat[row][col])
        writeTotalMat.write(rowStr + '\n')

    # Print the hand-in matrix info
    leftList = []
    rightList = []
    # List: a-z <-> 1-26
    for i in range(1, 27):
        if handCode[i] == '0':
            leftList.append(i)
        else:
            rightList.append(i)

    leftMatFileName = fileNamePrefix + '_left.csv'
    writeLeftMat = open(leftMatFileName, 'w')
    tag = [getChar(num) for num in leftList]
    tagStr = ',' + ','.join(tag)
    writeLeftMat.write(tagStr + '\n')
    for row in leftList:
        rowStr = getChar(row)
        for col in leftList:
            rowStr += ',' + str(numMat[row][col])
        writeLeftMat.write(rowStr + '\n')

    rightMatFileName = fileNamePrefix + '_right.csv'
    writeRightMat = open(rightMatFileName, 'w')
    tag = [getChar(num) for num in rightList]
    tagStr = ',' + ','.join(tag)
    writeRightMat.write(tagStr + '\n')
    for row in rightList:
        rowStr = getChar(row)
        for col in rightList:
            rowStr += ',' + str(numMat[row][col])
        writeRightMat.write(rowStr + '\n')

texts = loadTestingSet()
testingSetSize = len(texts)

# Character-Pair Matrix
numMat = [[0 for col in range(27)] for row in range(27)]
listMat = [[[] for col in range(27)] for row in range(27)]

# Parse the [a-z]*[a-z] matrix
for textNo in range(testingSetSize):
    currText = texts[textNo]
    textLen = len(currText)
    for i in range(textLen - 1):
        rowNo = getCharNo(currText[i])
        colNo = getCharNo(currText[i + 1])
        numMat[rowNo][colNo] += 1
        listMat[rowNo][colNo].append(textNo)

# Check number
printMatrixInfo('testingSetNumMat', numMat, listMat)

# Calculate the importance of each text 
textImportance = [0.0 for i in range(testingSetSize)]
for textNo in range(testingSetSize):
    currText = texts[textNo]
    textLen = len(currText)
    for i in range(textLen - 1):
        rowNo = getCharNo(currText[i])
        colNo = getCharNo(currText[i + 1])
        textImportance[textNo] += 1.0 / numMat[rowNo][colNo]

print textImportance