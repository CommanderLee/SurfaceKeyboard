# Calculate Keyboard Layout( Size and Coordinate )
# Zhen Li, Tsinghua University.
import matplotlib.pyplot as plt
from pylab import *

def calcKeyboardLayout():
    "Calculate Keyboard Layout( Size and Coordinate )"

    letterRow = ['qwertyuiop', 'asdfghjkl', 'zxcvbnm']
    # Data from Surface 2.0 standard keyboard.

    # Coordinate for 'Q','P'x, 'Space'y
    startX = 98.0
    startY = 59.0
    endX = 543.0
    endY = 247.0

    # Normalization
    rangeX = endX - startX
    rangeY = endY - startY

    # Coordinate for 'Q', 'A', 'Z'
    qX1, qY1 = 98.0, 59.0
    qX2, qY2 = 138.0, 103.0
    aX1, aY1 = 120.0, 109.0
    aX2, aY2 = 160.0, 152.0
    zX1, zY1 = 143.0, 157.0
    zX2, zY2 = 183.0, 200.0
    gapX = 45

    y = [(qY1 + qY2) / 2, (aY1 + aY2) / 2, (zY1 + zY2) / 2]
    y = [(yi - startY) / rangeY for yi in y]

    x = [[], [], []]
    x[0] = [((qX1 + qX2) / 2 + i * gapX - startX) / rangeX for i in range(0, 10)]
    x[1] = [((aX1 + aX2) / 2 + i * gapX - startX) / rangeX for i in range(0, 9)]
    x[2] = [((zX1 + zX2) / 2 + i * gapX - startX) / rangeX for i in range(0, 7)]

    # Position of each letter in normalized layout
    letterPosX = range(0, 26)
    letterPosY = range(0, 26)

    figure(0)
    for row in range(0, 3):
        colNum = len(x[row])
        for col in range(0, colNum):
            currLetter = letterRow[row][col]
            currNo = ord(currLetter) - ord('a')
            letterPosX[currNo] = x[row][col]
            letterPosY[currNo] = y[row]

            plt.text(x[row][col], y[row], currLetter,
                verticalalignment = 'center', horizontalalignment = 'center',
                color = 'b', fontsize = 15)
    return [letterPosX, letterPosY]

if __name__ == '__main__':
    [letterPosX, letterPosY] = calcKeyboardLayout()
    print letterPosX
    print letterPosY
    title('Normalized Layout')
    gca().invert_yaxis()
    show()




