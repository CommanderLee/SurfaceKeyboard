# Calculate Words Per Minute (WPM) for different conditions
# Zhen Li, Tsinghua University.

import Tkinter, tkFileDialog
import os
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

def parseData(dataCSV, isKbd):
    "Parse data file from different conditions(format)"

    # PhyKbd Format: RawInput,TypingTime
    if isKbd:
        pass

    # Hand Touch Format: X, Y, Time, TaskNo-PointNo-FingerId, PointType
    else:
        pass

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
    if (string.find(os.path.basename(dataFile.name).split('.')[0], 'PhyKbd') == -1):
        isKbd = False
    parseData(dataCSV, isKbd)