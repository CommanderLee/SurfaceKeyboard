# Check some important features of the testing set.
# Zhen Li, Tsinghua University.
import numpy as np

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

    # Print the one-hand matrix info
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

def saveSelectedText(fileName, textNum, textContent, textTotalValue, textOneHandValue):
    "Save selected text based on their importance"
    textStruct = zip(textContent, textTotalValue, textOneHandValue)
    textStructArray = np.array(textStruct, dtype = [('text', 'S50'), ('totalValue', float), ('oneHandValue', float)])
    textNum = min(textNum, len(textContent))

    # Total Value Order
    writeText = open(fileName + '_MaxTotalValue.txt', 'w')
    textStructArray.sort(order = 'totalValue')
    descendTextList = textStructArray[::-1]['text'].tolist()
    
    for text in descendTextList[:textNum]:
        writeText.write(text + '\n')
    print textStructArray[::-1][:textNum]

    # One Hand Value Order
    writeText = open(fileName + '_MaxOneHandValue.txt', 'w')
    textStructArray.sort(order = 'oneHandValue')
    descendTextList = textStructArray[::-1]['text'].tolist()

    for text in descendTextList[:textNum]:
        writeText.write(text + '\n')
    print textStructArray[::-1][:textNum]

# Main Procedure #

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
# Total matrix value and one-hand matrix value
textTotalValue = [0.0 for i in range(testingSetSize)]
textOneHandValue = [0.0 for i in range(testingSetSize)]
for textNo in range(testingSetSize):
    currText = texts[textNo]
    textLen = len(currText)
    for i in range(textLen - 1):
        rowNo = getCharNo(currText[i])
        colNo = getCharNo(currText[i + 1])

        textTotalValue[textNo] += 1.0 / numMat[rowNo][colNo]
        if handCode[rowNo] == handCode[colNo]:
            textOneHandValue[textNo] += 1.0 / numMat[rowNo][colNo]

saveSelectedText('TaskText', 30, texts, textTotalValue, textOneHandValue)
