# Combine all data from Data-2(Exp2)
# Zhen Li, Tsinghua University

import Tkinter, tkFileDialog
import numpy as np 

def getED(src, dst):
    "Get the edit distance of two string"
    matD = np.zeros((len(src), len(dst)), int)
    for i in range(len(src)):
        matD[i][0] = i
    for j in range(len(dst)):
        matD[0][j] = j

    for i in range(1, len(src)):
        for j in range(1, len(dst)):
            minUpAndLeft = min(matD[i-1][j], matD[i][j-1]) + 1
            minUpLeft = matD[i-1][j-1]
            if src[i] != dst[j]:
                minUpLeft += 1
            matD[i][j] = min(minUpLeft, minUpAndLeft)

    return matD[len(src)-1][len(dst)-1]

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
    # result: Name, Mode, SessionNum, RawInput, TaskText, TypingTime, DeleteNumber, SelectSequence, SelectionNum, WordNum, DeleteCharNum, UncErrRate, WPM
    resultLines = []
    for openMajorCSVFile in openMajorCSVFiles:
        majorCSVFileName = openMajorCSVFile.name
        taskTextFileName = majorCSVFileName[0:len(majorCSVFileName)-9] + 'TaskText.txt'
        testCSVFileName = majorCSVFileName[0:len(majorCSVFileName)-9] + 'test.csv'
        testerName = majorCSVFileName.split('_')[4]
        testMode = majorCSVFileName.split('_')[6]
        print '(' + testerName + ', ' + testMode + ')'

        # Load 'xxx_TaskText.txt'
        taskTextFile = open(taskTextFileName, 'r')
        taskTexts = [text.strip().lower() for text in taskTextFile]
        print '    xxx_TaskText.txt'

        # Parse 'xxx_test.csv'
        testData = np.genfromtxt(testCSVFileName, dtype = None, delimiter = ',', names = True)
        RawInput = testData['RawInput']
        TypingTime = testData['TypingTime']
        DeleteNumber = testData['DeleteNumber']
        SelectSequence = [str(x) for x in testData['SelectSequence']]
        print SelectSequence
        majorData = np.genfromtxt(majorCSVFileName, dtype = None, delimiter = ',', names = True)
        dataId = [_id.strip() for _id in majorData['TaskIndex_PointIndex_FingerId']]
        dataType = [_type.strip() for _type in majorData['PointType']]

        SessionNum = []
        SelectionNum = []
        WordNum = []
        UncErrRate = []
        WPM = []
        DeleteCharNum = []

        taskIndex = 0
        pointIndex = 0
        for i in range(len(RawInput)):
            SessionNum.append(i / 10)

            if SelectSequence[i] == 'False' or SelectSequence[i] == '':
                SelectionNum.append(0)
            else:
                SelectionNum.append(len(SelectSequence[i].split('-')))
            WordNum.append(len(taskTexts[i].split(' ')))

            src = taskTexts[i]
            dst = RawInput[i]
            currED = getED(src, dst)
            UncErrRate.append(currED)
            print 'Task:%s\nUser:%s\nDist:%d' % (src, dst, currED)

            currWPM = (float)(len(RawInput) - 1) / (5 * TypingTime[i]) * 60000
            print 'WPM:%f\n\n' % (currWPM)
            WPM.append(currWPM)

            deleteCnt = 0
            while pointIndex < len(dataId):
                newTaskIndex = int(dataId[pointIndex].split('_')[0])
                if (newTaskIndex != taskIndex):
                    taskIndex = newTaskIndex
                    break
                else:
                    # Go on
                    if dataType[pointIndex] == 'Delete':
                        deleteCnt += 1
                    pointIndex += 1
            DeleteCharNum.append(deleteCnt)

        print '    xxx_test.csv'

        for i in range(len(RawInput)):
            resultLines.append('%s, %s, %d, %s, %s, %f, %d, %s, %d, %d, %d, %f, %f\n' %
                (testerName, testMode, SessionNum[i], RawInput[i], taskTexts[i], TypingTime[i], DeleteNumber[i], SelectSequence[i], SelectionNum[i], WordNum[i], DeleteCharNum[i], UncErrRate[i], WPM[i]))
    
    writeFile = open('AllData-%dpersons.csv' % (len(openMajorCSVFiles)/3), 'w')
    writeFile.write('Name, Mode, SessionNum, RawInput, TaskText, TypingTime, DeleteNumber, SelectSequence, SelectionNum, WordNum, DeleteCharNum, UncErrRate, WPM\n')
    for line in resultLines:
        writeFile.write(line)
