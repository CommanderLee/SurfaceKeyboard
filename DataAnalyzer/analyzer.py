import csv
import matplotlib.pyplot as plt
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

    # Mean position of space point
    spaceX = -1
    spaceY = -1

    # Position list of each letter (and spacebar)
    letterPosX = [[] for x in range(27)]
    letterPosY = [[] for x in range(27)]

    letterPosXFix = [[] for x in range(27)]
    letterPosYFix = [[] for x in range(27)]

    while textNo < textLen:
        # Parse every text
        currText = texts[textNo]
        print "%d: %s" % (textNo, currText)
        
        # Get Point List
        listX = []
        listY = []

        # Pick out 'Touch' point and ignore 'Move' point
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
        # subplot(2, 1, 1)
        minLen = min(len(currText), len(listX))
        # spaceXList = []
        # spaceYList = []
        for letterNo in range(0, minLen):
            if currText[letterNo] == ' ':
                # Space
                plot(listX[letterNo], listY[letterNo], colors[0])
                # spaceXList.append(listX[letterNo])
                # spaceYList.append(listY[letterNo])
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
            spaceX = np.mean(letterPosX[0])
            spaceY = np.mean(letterPosY[0])
            print 'Space: (%d, %d)' % (spaceX, spaceY)
        else:
            biasX = spaceX - np.mean(letterPosX[0])
            biasY = spaceY - np.mean(letterPosY[0])
            print 'Bias: (%d, %d)' % (biasX, biasY)
        for letterNo in range(0, minLen):
            if currText[letterNo] == ' ':
                # Space
                plot(listX[letterNo] + biasX, listY[letterNo] + biasY, colors[0])
                letterPosXFix[0].append(listX[letterNo] + biasX)
                letterPosYFix[0].append(listY[letterNo] + biasY)
            else:
                # Letters
                letter = currText[letterNo].lower()
                plot(listX[letterNo] + biasX, listY[letterNo] + biasY, colors[ord(letter) - ord('a') + 1])
                letterPosXFix[ord(letter) - ord('a') + 1].append(listX[letterNo] + biasX)
                letterPosYFix[ord(letter) - ord('a') + 1].append(listY[letterNo] + biasY)

        # Plot different test set with different colors
        figure(2)
        plot(listX, listY, colors[textNo % colorLen])

        # Break when finish processing the data set
        if dataNo >= dataLen:
            break

        textNo += 1


    # plot(dataX, dataY, 'b.')


    # Inverse the axis to fit the Surface Window
    figure(0)
    # Space:
    plt.text(np.mean(letterPosX[0]), np.mean(letterPosY[0]), 'space',
        verticalalignment = 'center', horizontalalignment = 'center',
        color = colors[0][0], fontsize = 25)
    # 'a' to 'z':
    for i in range(1, 27):
        if len(letterPosX[i]) > 0:
            plt.text(np.mean(letterPosX[i]), np.mean(letterPosY[i]), chr(ord('a') + i - 1),
                verticalalignment = 'center', horizontalalignment = 'center',
                color = colors[i][0], fontsize = 35)
        else:
            print 'Letter %s not found.' % (chr(ord('a') + i - 1))
    # subplot(2, 1, 1)
    title('Different Letters')
    gca().invert_yaxis()
    # legend()

    # subplot(2, 1, 2)
    figure(1)
    # Space:
    plt.text(np.mean(letterPosXFix[0]), np.mean(letterPosYFix[0]), 'space',
        verticalalignment = 'center', horizontalalignment = 'center',
        color = colors[0][0], fontsize = 25)
    # 'a' to 'z':
    for i in range(1, 27):
        if len(letterPosXFix[i]) > 0:
            plt.text(np.mean(letterPosXFix[i]), np.mean(letterPosYFix[i]), chr(ord('a') + i - 1),
                verticalalignment = 'center', horizontalalignment = 'center',
                color = colors[i][0], fontsize = 35)
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

    show()
