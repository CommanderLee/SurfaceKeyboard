# Analyze the gesture (enter, backspace .etc) parameters
# Zhen Li, Tsinghua University

import Tkinter, tkFileDialog
import numpy as np
import matplotlib.pyplot as plt

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

# Main Procedure
if __name__ == '__main__':
    openFiles = loadFiles()
    pntColors = ['b.-', 'c.-', 'g.-', 'k.-', 'm', 'c', 'y']

    moveDist = []
    moveTime = []
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
                    print moveTime
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
        

    plt.gca().invert_yaxis()
    plt.show()
