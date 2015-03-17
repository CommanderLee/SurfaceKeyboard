# Analyze the data generated from the Surface App.
# Zhen Li, Tsinghua University.
import csv
import matplotlib.pyplot as plt
from pylab import *
import Tkinter, tkFileDialog
import numpy as np 

# Read word list
textFile = open('TaskText.txt', 'r')
texts = [text.strip() for text in textFile]
# print texts

# Load data files
tkObj = Tkinter.Tk()
tkObj.file_opt = options = {}
options['defaultextension'] = '.csv'

openFiles = tkFileDialog.askopenfiles('r')
if openFiles:
    # Constants
    # Color constant of space & 26 letters
    colors1 = ['b.', 'c.', 'g.', 'k.', 'm.', 'r.', 'y.', 
    'bo', 'co', 'go', 'ko', 'mo', 'ro', 'yo', 
    'b*', 'c*', 'g*', 'k*', 'm*', 'r*', 'y*',
    'b<', 'c<', 'g<', 'k<', 'm<', 'r<']
    colors = ['b.', 'c.', 'g.', 'k.', 'm.', 'c.', 'y.', 
    'b.', 'c.', 'g.', 'k.', 'm.', 'r.', 'y.', 
    'b.', 'c.', 'g.', 'k.', 'b.', 'r.', 'r.', 
    'b.', 'c.', 'g.', 'k.', 'm.', 'r.']
    colorLen = len(colors)

    # Common variables
    # Position list of each letter (and spacebar)
    letterPosX = [[] for x in range(27)]
    letterPosY = [[] for x in range(27)]

    letterPosXFix = [[] for x in range(27)]
    letterPosYFix = [[] for x in range(27)]

    # Mean position of space point (base point)
    spaceX = -1
    spaceY = -1

    # Parse each file
    for dataFile in openFiles:
        print dataFile.name + ':'

        # X, Y, Time, TaskNo-PointNo-FingerId, PointType
        # Hint: '-' will be removed by the function numpy.genfromtxt 
        # (ref: http://docs.scipy.org/doc/numpy/user/basics.io.genfromtxt.html)
        data = np.genfromtxt(dataFile.name, dtype = None, delimiter = ',', names = True)

        # Load data column
        dataX = data['X']
        dataY = data['Y']
        dataTime = data['Time']
        dataId = [_id.strip() for _id in data['TaskNoPointNoFingerId']]
        dataType = [_type.strip() for _type in data['PointType']]

        textNo = int(dataId[0].split('-')[0])
        textLen = len(texts)
        dataNo = 0
        dataLen = len(data)

        touchTime = []

        while textNo < textLen:
            # Parse every text (sentence)
            currText = texts[textNo]
            print "%d: %s" % (textNo, currText)
            
            # Get point list of current sentence
            listX = []
            listY = []
            # Position list of spacebar for current sentence
            # Use this info to fix the bias
            spaceXList = []
            spaceYList = []

            # Calculate the touching time for every point
            touchStart = -1

            # Get all 'Touch' point and ignore 'Move' point
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

                    if touchStart >= 0:
                        touchTime.append(dataTime[dataNo-1] - touchStart)
                        if dataTime[dataNo-1] - touchStart < 20:
                            print 'Exception: ' + dataId[dataNo-1]
                    touchStart = dataTime[dataNo]
                dataNo += 1
                
            if len(currText) != len(listX):    
                print "Warning: %d letters, %d touch points." % (len(currText), len(listX))

            figure(0)
            # Plot different letters with different colors
            # TODO: Remove the wrong point
            # subplot(2, 1, 1)
            minLen = min(len(currText), len(listX))
            for letterNo in range(0, minLen):
                if currText[letterNo] == ' ':
                    # Space
                    plot(listX[letterNo], listY[letterNo], colors[0])
                    spaceXList.append(listX[letterNo])
                    spaceYList.append(listY[letterNo])
                    letterPosX[0].append(listX[letterNo])
                    letterPosY[0].append(listY[letterNo])
                else:
                    # Letters
                    letter = currText[letterNo].lower()
                    plot(listX[letterNo], listY[letterNo], colors[ord(letter) - ord('a') + 1], 
                        label = letter)
                    letterPosX[ord(letter) - ord('a') + 1].append(listX[letterNo])
                    letterPosY[ord(letter) - ord('a') + 1].append(listY[letterNo])

            # Fix the data
            figure(1)
            # subplot(2, 1, 2)
            biasX = 0
            biasY = 0
            if spaceX == -1:
                spaceX = np.mean(spaceXList)
                spaceY = np.mean(spaceYList)
                print 'Space: (%d, %d)' % (spaceX, spaceY)
            else:
                biasX = spaceX - np.mean(spaceXList)
                biasY = spaceY - np.mean(spaceYList)
                print 'Bias: (%d, %d)' % (biasX, biasY)
            for letterNo in range(0, minLen):
                xFix = listX[letterNo] + biasX
                yFix = listY[letterNo] + biasY
                if currText[letterNo] == ' ':
                    # Space
                    plot(xFix, yFix, colors[0])
                    letterPosXFix[0].append(xFix)
                    letterPosYFix[0].append(yFix)
                else:
                    # Letters
                    letter = currText[letterNo].lower()
                    letterNo = ord(letter) - ord('a') + 1
                    plot(xFix, yFix, colors[letterNo])
                    letterPosXFix[letterNo].append(xFix)
                    letterPosYFix[letterNo].append(yFix)

            # Plot different test set with different colors
            figure(2)
            plot(listX, listY, colors[textNo % colorLen])

            # Break when finish processing the data set
            if dataNo >= dataLen:
                break

            textNo += 1

        figure(3)
        plot(touchTime, label = dataFile.name)
        print touchTime
        print 'Touchtime: mean:%f, std:%f, median:%f, max:%f, min:%f' % (np.mean(touchTime), 
            np.std(touchTime), np.median(touchTime), np.max(touchTime), np.min(touchTime))

    # plot(dataX, dataY, 'b.')


    # Inverse the axis to fit the Surface Window
    figure(0)
    # Space:
    plt.text(np.median(letterPosX[0]), np.median(letterPosY[0]), 'space',
        verticalalignment = 'center', horizontalalignment = 'center',
        color = colors[0][0], fontsize = 45)
    # 'a' to 'z':
    for i in range(1, 27):
        if len(letterPosX[i]) > 0:
            plt.text(np.median(letterPosX[i]), np.median(letterPosY[i]), chr(ord('a') + i - 1),
                verticalalignment = 'center', horizontalalignment = 'center',
                color = colors[i][0], fontsize = 45)
        else:
            print 'Letter %s not found.' % (chr(ord('a') + i - 1))
    # subplot(2, 1, 1)
    title('Different Letters')
    gca().invert_yaxis()
    # legend()

    # subplot(2, 1, 2)
    figure(1)
    # Space:
    plt.text(np.median(letterPosXFix[0]), np.median(letterPosYFix[0]), 'space',
        verticalalignment = 'center', horizontalalignment = 'center',
        color = colors[0][0], fontsize = 45)
    # 'a' to 'z':
    for i in range(1, 27):
        if len(letterPosXFix[i]) > 0:
            plt.text(np.median(letterPosXFix[i]), np.median(letterPosYFix[i]), chr(ord('a') + i - 1),
                verticalalignment = 'center', horizontalalignment = 'center',
                color = colors[i][0], fontsize = 45)
        else:
            print 'Letter %s not found.' % (chr(ord('a') + i - 1))
    # fig1.text(3, 8, 'boxed italics text in data coords', style='italic',
    #     bbox={'facecolor':'red', 'alpha':0.5, 'pad':10})
    # text(spaceX, spaceY, 'space', fontsize = 15)
    # plt.text(spaceX, spaceY, 'space',
    #     verticalalignment = 'center', horizontalalignment = 'center',
    #     color = colors[0][0], fontsize = 25)
    title('Different Letters - bias')
    gca().invert_yaxis()

    figure(2)
    title('Different Text No.')
    gca().invert_yaxis()

    figure(3)
    legend()

    show()
