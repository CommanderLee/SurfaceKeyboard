import csv
from pylab import *
import Tkinter, tkFileDialog
import numpy as np 

# Read word list
textFile = open('TaskText.txt', 'r')
texts = [text.strip() for text in textFile]
# print texts

# Read data
tkObj = Tkinter.Tk()
tkObj.file_opt = options = {}
options['defaultextension'] = '.csv'

dataFile = tkFileDialog.askopenfile('r')
if dataFile:
    # X, Y, Time, TaskNo-PointNo-FingerId, PointType
    # Hint: '-' will be removed by the function numpy.genfromtxt 
    # (ref: http://docs.scipy.org/doc/numpy/user/basics.io.genfromtxt.html)
    data = np.genfromtxt(dataFile.name, dtype = None, delimiter = ',', names = True)

    # Load data column
    dataX = data['X']
    dataY = data['Y']
    dataId = [_id.strip() for _id in data['TaskNoPointNoFingerId']]
    dataType = [_type.strip() for _type in data['PointType']]

    # Color constant of space & 26 letters
    colors = ['b.', 'c.', 'g.', 'k.', 'm.', 'r.', 'y.', 
    'bo', 'co', 'go', 'ko', 'mo', 'ro', 'yo', 
    'b*', 'c*', 'g*', 'k*', 'm*', 'r*', 'y*',
    'b<', 'c<', 'g<', 'k<', 'm<', 'r<']

    figure()

    textSt = 0
    textEd = 20
    dataNo = 0
    dataLen = len(data)
    for textNo in range(textSt, textEd):
        # Parse every text
        currText = texts[textNo]
        print "%d: %s" % (textNo, currText)
        
        # Get Point List
        listX = []
        listY = []

        while dataNo < dataLen:
            idList = dataId[dataNo].split('-')
            # print idList
            taskNo = int(idList[0])
            # Go to next text
            if taskNo > textNo:
                break

            # pointNo = idList[1]
            # fingerId = idList[2]
            if dataType[dataNo] == 'Touch':
                listX.append(dataX[dataNo])
                listY.append(dataY[dataNo])
            dataNo += 1
            
        print "%d letters, %d touch points." % (len(currText), len(listX))

        # TODO: Remove the wrong point
        minLen = min(len(currText), len(listX))
        for letterNo in range(0, minLen):
            if currText[letterNo] == ' ':
                plot(listX[letterNo], listY[letterNo], colors[0])
            else:
                plot(listX[letterNo], listY[letterNo], colors[ord(currText[letterNo].lower()) - ord('a') + 1])


    # plot(dataX, dataY, 'b.')


    # Inverse the axis to fit the Surface Window
    gca().invert_yaxis()
    show()
