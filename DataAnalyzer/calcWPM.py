# Calculate Words Per Minute (WPM) for different conditions
# Zhen Li, Tsinghua University.

import Tkinter, tkFileDialog
import os
import string
import numpy as np

def loadFiles():
    "Load data files of different conditions"
    dataFileName = ''
    textFileName = ''

    tkObj = Tkinter.Tk()
    tkObj.file_opt = options = {}
    options['title'] = 'Select Data File and TaskText File'
    openFiles = tkFileDialog.askopenfiles('r')

    if openFiles:
        for openFile in openFiles:
            # fileName = os.path.basename(dataFile.name).split('.')[0]
            fileExt = os.path.splitext(openFile.name)[1]
            if fileExt == '.csv':
                dataFileName = openFile.name
            elif fileExt == '.txt':
                textFileName = openFile.name
        
        return [dataFileName, textFileName]
    
    else:
        print 'Error: Cannot Load Files.'

def parseData(dataCSV, isKbd, textList):
    "Parse data file from different conditions(format)"

    # Character Counter and Time Counter (s)
    charCnt = 0
    timeCnt = 0.0
    WPM = 0.0

    # PhyKbd Format: RawInput,TypingTime
    if isKbd:
        dataInput = dataCSV['RawInput']
        dataTime = dataCSV['TypingTime']

        for [myInput, myTime, text] in zip(dataInput, dataTime, textList):
            charCnt += len(myInput)
            timeCnt += myTime / 1000.0
            # TODO: Calculate error rate

    # Hand Touch Format: X, Y, Time, TaskNo-PointNo-FingerId, PointType
    else:
        dataTime = dataCSV['Time']
        dataId = [_id.strip() for _id in dataCSV['TaskNoPointNoFingerId']]
        dataType = [_type.strip() for _type in dataCSV['PointType']]

        currChar = 0
        currTime = 0.0
        startTime = 0.0
        for [myTime, myId, myType] in zip(dataTime, dataId, dataType):
            if myType == 'Touch':
                idList = myId.split('-')
                pointNo = int(idList[1])

                # A new Start
                if pointNo == 0:
                    # Not the 1st sentence
                    if currChar != 0:
                        charCnt += currChar
                        timeCnt += currTime / 1000.0
                        currChar = 0
                        currTime = 0.0

                    startTime = myTime

                else:
                    currChar = pointNo + 1
                    currTime = myTime - startTime
        # The last sentence
        if currChar != 0:
            charCnt += currChar
            timeCnt += currTime / 1000.0
            currChar = 0
            currTime = 0.0      

    WPM = charCnt / timeCnt * 60 / 5
    return WPM

# Main Procedure
if __name__ == '__main__':
    [dataFileName, textFileName] = loadFiles()
    print 'Load files: %s' % ([dataFileName, textFileName])

    # Read data file and text file
    dataCSV = np.genfromtxt(dataFileName, dtype = None, delimiter = ',', names = True)
    rawText = open(textFileName, 'r')
    textList = [text.strip() for text in rawText]
    
    # If this is the physical keyboard data file
    isKbd = True
    if (string.find(os.path.basename(dataFileName).split('.')[0], 'PhyKbd') == -1):
        isKbd = False
    WPM = parseData(dataCSV, isKbd, textList)

    print 'WPM: %f' % (WPM)