# Some functions about standard keyboard and its layout.
# Zhen Li, Tsinghua University.
import matplotlib.pyplot as plt
from pylab import *
from constants import *

def calcKeyboardLayout():
    "Calculate Keyboard Layout( Size and Coordinate )"

    letterRow = ['qwertyuiop', 'asdfghjkl', 'zxcvbnm']
    # Data from Surface 2.0 standard keyboard.

    # Coordinate for 'Q','P'x, 'Space'y
    startX = 98.0
    startY = 59.0
    endX = 543.0
    endY = 247.0

    # TODO: How to do normalization?
    # rangeX = endX - startX
    # rangeY = endY - startY

    # Coordinate for 'Q', 'A', 'Z'
    qX1, qY1 = 98.0, 59.0
    qX2, qY2 = 138.0, 103.0
    aX1, aY1 = 120.0, 109.0
    aX2, aY2 = 160.0, 152.0
    zX1, zY1 = 143.0, 157.0
    zX2, zY2 = 183.0, 200.0
    gapX = 45

    y = [(qY1 + qY2) / 2, (aY1 + aY2) / 2, (zY1 + zY2) / 2]
    y = [(yi - startY) for yi in y]

    x = [[], [], []]
    x[0] = [((qX1 + qX2) / 2 + i * gapX - startX) for i in range(0, 10)]
    x[1] = [((aX1 + aX2) / 2 + i * gapX - startX) for i in range(0, 9)]
    x[2] = [((zX1 + zX2) / 2 + i * gapX - startX) for i in range(0, 7)]

    # Position of each letter in normalized layout
    posX = range(0, 26)
    posY = range(0, 26)

    figure(0)
    for row in range(0, 3):
        colNum = len(x[row])
        for col in range(0, colNum):
            currLetter = letterRow[row][col]
            currNo = ord(currLetter) - ord('a')
            posX[currNo] = x[row][col]
            posY[currNo] = y[row]

            plot(x[row][col], y[row], 'ro', markersize = 40)
            plt.text(x[row][col], y[row], currLetter,
                verticalalignment = 'center', horizontalalignment = 'center',
                color = 'b', fontsize = 15)
    return [posX, posY]

def calcWordVec(word):
    "Calculate word vector & points within each hand"
    # Point and Vector list within Left/Right hand
    pntIdL, pntIdR = [], []
    vecL, vecR = [], []

    for char in word:
        charNo = ord(char) - ord('a')
        if handCode[charNo] == '0':
            if len(pntIdL) > 0:
                vecL.append((letterPosX[charNo] - letterPosX[pntIdL[-1]], letterPosY[charNo] - letterPosY[pntIdL[-1]]))
            pntIdL.append(charNo)
        else:
            if len(pntIdR) > 0:
                vecR.append((letterPosX[charNo] - letterPosX[pntIdR[-1]], letterPosY[charNo] - letterPosY[pntIdR[-1]]))
            pntIdR.append(charNo)

    return [pntIdL, pntIdR, vecL, vecR]

if __name__ == '__main__':
    [posX, posY] = calcKeyboardLayout()
    print posX
    print posY
    title('Surface Keyboard Layout')
    gca().invert_yaxis()
    show()

    [pntIdL, pntIdR, vecL, vecR] = calcWordVec('please')
    print [pntIdL, pntIdR, vecL, vecR]




