# Constants
# Zhen Li, Tsinghua University.

# Calculated from keyboatdLayoutHelper.calcKeyboardLayout()
letterPosX = [42.0, 245.0, 155.0, 132.0, 110.0, 177.0, 222.0, 267.0, 335.0, 312.0, 357.0, 402.0, 335.0, 290.0, 380.0, 425.0, 20.0, 155.0, 87.0, 200.0, 290.0, 200.0, 65.0, 110.0, 245.0, 65.0]
letterPosY = [71.5, 119.5, 119.5, 71.5, 22.0, 71.5, 71.5, 71.5, 22.0, 71.5, 71.5, 71.5, 119.5, 119.5, 22.0, 22.0, 22.0, 22.0, 71.5, 22.0, 22.0, 119.5, 22.0, 119.5, 22.0, 119.5]

# Pre-processing: Encode the words. 0:left, 1:right, 2:spacebar.
# a b c d e f g
# h i j k l m n
# o p q r s t u v w x y z
handCode = ['0', '0', '0', '0', '0', '0', '0', 
'1', '1', '1', '1', '1', '1', '1',
'1', '1', '0', '0', '0', '0', '1', '0', '0', '0', '1', '0']