# Combine all data from Data(Exp1)
# Zhen Li, Tsinghua University

import Tkinter, tkFileDialog
import numpy as np 

# Main:

# Load user data files
tkObj = Tkinter.Tk()
tkObj.file_opt = options = {}
options['defaultextension'] = '.csv'

# Only accept 'xxx_major.csv'
openFiles = tkFileDialog.askopenfiles('r')
if openFiles:
    openMajorCSVFiles = []

    # Parse and log 'xxx_major.csv'
    for openFile in openFiles:
        flag = openFile.name[len(openFile.name)-9:len(openFile.name)-4]
        if flag == 'major':
            openMajorCSVFiles.append(openFile)
    print 'Load %d data sets.' % (len(openMajorCSVFiles))

    # Start:
    # major-result: Name, Index, InputLen, TypingTime, DeleteCharNum, CorErrRate, WPM
    # phy-result: Name, Index, WPM, CorErrRate, UncErrRate
    majorResults = []
    phyResults = []
    for openMajorCSVFile in openMajorCSVFiles:
        majorCSVFileName = openMajorCSVFile.name
        taskTextFileName = majorCSVFileName[0:len(majorCSVFileName)-9] + 'TaskText.txt'
        
        testerName = majorCSVFileName.split('_')[4]
        phyKbdTestName = majorCSVFileName.split('_')[0][:-5] + testerName + '.csv'
        
        print phyKbdTestName
        
        # Load 'xxx_TaskText.txt'
        taskTextFile = open(taskTextFileName, 'r')
        taskTexts = [text.strip().lower() for text in taskTextFile]
        print '    xxx_TaskText.txt'

        # Parse 'username.csv'
        testData = np.genfromtxt(phyKbdTestName, dtype = None, delimiter = ' ', names = True)

        TestingFlag = testData['Testing']
        WPM = testData['WPM']
        CorErrRate = testData['CorErrRate']
        UncErrRate = testData['UncErrRate']

        for i in range(len(TestingFlag)):
            if TestingFlag[i] == 1:
                phyResults.append('%s, %d, %f, %f, %f\n' % (testerName, i+1, WPM[i], CorErrRate[i], UncErrRate[i]))

        majorData = np.genfromtxt(majorCSVFileName, dtype = None, delimiter = ',', names = True)
        dataTime = majorData['Time']
        dataId = [_id.strip() for _id in majorData['TaskIndex_PointIndex_FingerId']]
        dataType = [_type.strip() for _type in majorData['PointType']]

        touchCnt = 0
        deleteCnt = 0
        startTime = 0

        taskIndex = 0
        pointIndex = 0
        for i in range(len(dataId)):
            newTaskIndex = int(dataId[i].split('_')[0])

            if (newTaskIndex != taskIndex) or (i == len(dataId)-1):
                typeTime = dataTime[i-1] - startTime
                if i == len(dataId) - 1:
                    typeTime = dataTime[i] - startTime
                    if dataType[i] == 'Touch':
                        touchCnt += 1
                    elif dataType[i] == 'Delete':
                        deleteCnt += 1

                corErrRate = (float)(deleteCnt) / touchCnt
                wpm = (float)(touchCnt - 1) / (5 * typeTime) * 60000

                # Save: Name, Index, InputLen, TypingTime, DeleteCharNum, CorErrRate, WPM
                majorResults.append('%s, %d, %d, %f, %d, %f, %f\n' 
                    % (testerName, taskIndex, touchCnt, typeTime, deleteCnt, corErrRate,wpm))
                
                # Clear
                taskIndex = newTaskIndex
                touchCnt = 0
                deleteCnt = 0
                startTime = 0

            # Go on
            if dataType[i] == 'Touch':
                touchCnt += 1
            elif dataType[i] == 'Delete':
                deleteCnt += 1

    
    writeFile = open('Result/AllData-PhyKbd-%dpersons.csv' % (len(openMajorCSVFiles)), 'w')
    writeFile.write('Name, Index, WPM, CorErrRate, UncErrRate\n')
    for line in phyResults:
        writeFile.write(line)

    writeFile = open('Result/AllData-Major-%dpersons.csv' % (len(openMajorCSVFiles)), 'w')
    writeFile.write('Name, Index, InputLen, TypingTime, DeleteCharNum, CorErrRate, WPM\n')
    for line in majorResults:
        writeFile.write(line)
