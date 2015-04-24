# Analyze the gesture (enter, backspace .etc) parameters
# Zhen Li, Tsinghua University

import Tkinter, tkFileDialog
import numpy as np
import matplotlib.pyplot as plt
import math

def loadFiles():
    "Load data files of gestures"

    tkObj = Tkinter.Tk()
    tkObj.file_opt = options = {}
    options['title'] = 'Select Data Files'
    openFiles = tkFileDialog.askopenfiles('r')

    if openFiles:
        return openFiles
    else:
        print 'Error: Cannot Load Files.'

def getDistance(xList, yList):
    "Calculate distance from (x0,y0) to (xn,yn)"

    if len(xList) > 0 and len(xList) == len(yList):
        lastX = xList[0]
        lastY = yList[0]
        sumDist = 0.0
        for [x, y] in zip(xList[1:], yList[1:]):
            sumDist += math.sqrt(math.pow(x - lastX, 2) + math.pow(y - lastY, 2))
            lastX = x
            lastY = y
        return sumDist
    else:
        print 'Error: point lists X and Y have different length.'

# Main Procedure
if __name__ == '__main__':
    openFiles = loadFiles()
    pntColors = ['b.-', 'c.-', 'g.-', 'k.-', 'm', 'c', 'y']

    moveDistList = []
    moveTimeList = []
    for [openFile, pntColor] in zip(openFiles, pntColors):
        # Read to NumPy Array
        # X, Y, Time, TaskNo-PointNo-FingerId, PointType
        dataCSV = np.genfromtxt(openFile.name, dtype = None, delimiter = ',', names = True)
        
        # Relative X and Y coordinates. 
        relX = []
        relY = []
        startX = 0
        startY = 0
        startTime = 0.0
        moveTime = 0.0
        for [xi, yi, timei, idi, typei] in zip(dataCSV['X'], dataCSV['Y'], dataCSV['Time'], dataCSV['TaskNoPointNoFingerId'], dataCSV['PointType']):
            # A new start
            if typei.strip() == 'Touch':
                # Save last move
                if len(relX) > 0:
                    plt.figure(0)
                    plt.plot(relX, relY, pntColor)
                    # Filter: throw errors
                    if len(relX) > 3:
                        moveDistList.append(getDistance(relX, relY))
                        moveTimeList.append(moveTime)
                    else:
                        print 'Warning: Ignore point lists:\n   X:%s\n   Y:%s\n' % (relX, relY)
                    relX = []
                    relY = []
                # Start a new move
                startX = xi
                startY = yi
                startTime = timei
                
            relX.append(xi - startX)
            relY.append(yi - startY)
            moveTime = timei - startTime

        # Save distance and time to file
        saveFileName = openFile.name.split('.')[0] + '_Result.csv'
        saveFile = open(saveFileName, 'w')
        saveFile.write('Distance, Time\n')
        for [moveDist, moveTime] in zip(moveDistList, moveTimeList):
            saveFile.write('%f, %f\n' % (moveDist, moveTime))

    plt.gca().invert_yaxis()
    plt.show()
