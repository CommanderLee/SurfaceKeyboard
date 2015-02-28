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
    # print dataFile.name
    # X, Y, Time, TaskNo-PointNo-FingerId, PointType
    data = np.genfromtxt(dataFile.name, delimiter = ',', names = True)
    # print data['X']
    

'''
old reader block

reader = csv.reader(open("01-29_16_11_49_Typing7.csv"))
# for _x, _y, _time, _id, _type in reader:
    # print _x, _y
lines = [line for line in reader]
# print lines
_x = [float(line[0]) for line in lines[1:]]
_y = [float(line[1]) for line in lines[1:]]
_time = [float(line[2]) for line in lines[1:]]
_id = [line[3] for line in lines[1:]]
_type = [line[4] for line in lines[1:]]
# print _x
# print _y
# print _time
# print _id
# print _type
'''

