# Constants
# Zhen Li, Tsinghua University.

# Calculated from keyboatdLayoutHelper.calcKeyboardLayout()
letterPosX = [743.5, 928.5, 846.5, 825.5, 806.5, 866.5, 907.5, 948.5, 1011.5, 989.5, 1030.5, 1071.5, 1010.5, 969.5, 1052.5, 1093.5, 724.5, 847.5, 784.5, 888.5, 970.5, 887.5, 765.5, 805.5, 929.5, 764.5]
letterPosY = [617.4, 660.4, 660.4, 617.4, 572.4, 617.4, 617.4, 617.4, 572.4, 617.4, 617.4, 617.4, 660.4, 660.4, 572.4, 572.4, 572.4, 572.4, 617.4, 572.4, 572.4, 660.4, 572.4, 660.4, 572.4, 660.4]
# Generated from keyboatdLayoutHelper.loadKeyboardLayout('16users_kbd_pos.csv')
userPosX = [718.57701378, 909.28646996, 833.47336852, 813.78691024, 798.0410731, 853.90674048, 891.43037533, 936.84420906, 1005.498639, 970.26316325, 1017.0088152, 1057.8388884, 992.81661424, 953.96573418, 1046.8064112, 1091.973913, 709.51111111, 840.63848851, 767.88189101, 880.79090006, 962.8251284, 875.36052006, 755.24576271, 789.71264368, 926.77008268, 747.06666667]
userPosY = [608.76643043, 652.66137654, 649.99647352, 607.69583868, 567.42299606, 611.19359811, 610.85930762, 613.12794869, 566.97748005, 618.57894951, 605.59700376, 604.27777744, 652.05537706, 654.45690642, 565.00218248, 565.54347746, 574.67778049, 571.56836324, 609.71508114, 574.24703991, 571.10992157, 652.48759038, 570.5338952, 651.43678021, 571.14670341, 648.51999837]

# y = ax + b. Between two hands.
paramA = 2.903950
paramB = -2051.836766

# Key-Size
keySizeX = 39
keySizeY = 42
keySizeLen = 57.3149195236284

# Pre-processing: Encode the words. 0:left, 1:right, 2:spacebar.
# spacebar a b c d e f g
# h i j k l m n
# o p q r s t u v w x y z
handCode = ['2', '0', '0', '0', '0', '0', '0', '0', 
'1', '1', '1', '1', '1', '1', '1',
'1', '1', '0', '0', '0', '0', '1', '0', '0', '0', '1', '0']