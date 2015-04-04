# Check some important features of the testing set.
# Zhen Li, Tsinghua University.

from filesHelper import loadTestingSet

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

texts = loadTestingSet()
testingSetSize = len(texts)

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
