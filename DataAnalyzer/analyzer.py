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
    colorLen = len(colors)

    textNo = int(dataId[0].split('-')[0])
    textLen = len(texts)
    dataNo = 0
    dataLen = len(data)
    while textNo < textLen:
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
            
        if len(currText) != len(listX):    
            print "Warning: %d letters, %d touch points." % (len(currText), len(listX))

        figure(0)
        # Plot different letters with different colors
        # TODO: Remove the wrong point
        subplot(2, 1, 1)
        minLen = min(len(currText), len(listX))
        for letterNo in range(0, minLen):
            if currText[letterNo] == ' ':
                plot(listX[letterNo], listY[letterNo], colors[0])
            else:
                plot(listX[letterNo], listY[letterNo], colors[ord(currText[letterNo].lower()) - ord('a') + 1])

        # Fix the data
        subplot(2, 1, 2)

        # Plot different test set with different colors
        figure(1)
        plot(listX, listY, colors[textNo % colorLen])

        # Break when finish processing the data set
        if dataNo >= dataLen:
            break

        textNo += 1


    # plot(dataX, dataY, 'b.')


    # Inverse the axis to fit the Surface Window
    figure(0)
    subplot(2, 1, 1)
    title('Different Letters')
    gca().invert_yaxis()
    
    subplot(2, 1, 2)
    

    figure(1)
    title('Different Text No.')
    gca().invert_yaxis()

    show()
