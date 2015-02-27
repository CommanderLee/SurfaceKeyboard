import csv

# Read data
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