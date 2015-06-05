using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Newtonsoft.Json;

namespace SurfaceKeyboard
{
    class WordPredictor
    {
        public bool                                 loadStatus;
        public Dictionary<string, int>              freqDict;
        public Dictionary<string, List<string>>     codeSet;
        public Dictionary<string, VectorParameter>  vecParams;

        // Pre-processing: Encode the words. 0:left, 1:right, 2:spacebar.
        // spacebar a b c d e f g
        // h i j k l m n
        // o p q r s t u v w x y z
        static string[] handCode = {"2", "0", "0", "0", "0", "0", "0", "0", 
        "1", "1", "1", "1", "1", "1", "1",
        "1", "1", "0", "0", "0", "0", "1", "0", "0", "0", "1", "0"};

        // Calculated from Standard Keyboard Image
        static double[] letterPosX = new double[] { 743.5, 928.5, 846.5, 825.5, 806.5, 866.5, 907.5, 948.5, 1011.5, 989.5, 1030.5, 1071.5, 
            1010.5, 969.5, 1052.5, 1093.5, 724.5, 847.5, 784.5, 888.5, 970.5, 887.5, 765.5, 805.5, 929.5, 764.5};
        static double[] letterPosY = new double[] { 617.4, 660.4, 660.4, 617.4, 572.4, 617.4, 617.4, 617.4, 572.4, 617.4, 617.4, 617.4, 
            660.4, 660.4, 572.4, 572.4, 572.4, 572.4, 617.4, 572.4, 572.4, 660.4, 572.4, 660.4, 572.4, 660.4};
        
        // Generated from ('16users_kbd_pos.csv')
        static double[] userPosX = new double[] { 718.57701378, 909.28646996, 833.47336852, 813.78691024, 798.0410731, 853.90674048, 891.43037533, 
            936.84420906, 1005.498639, 970.26316325, 1017.0088152, 1057.8388884, 992.81661424, 953.96573418, 1046.8064112, 1091.973913, 
            709.51111111, 840.63848851, 767.88189101, 880.79090006, 962.8251284, 875.36052006, 755.24576271, 789.71264368, 926.77008268, 
            747.06666667 };
        static double[] userPosY = new double[] { 608.76643043, 652.66137654, 649.99647352, 607.69583868, 567.42299606, 611.19359811, 610.85930762, 
            613.12794869, 566.97748005, 618.57894951, 605.59700376, 604.27777744, 652.05537706, 654.45690642, 565.00218248, 565.54347746, 
            574.67778049, 571.56836324, 609.71508114, 574.24703991, 571.10992157, 652.48759038, 570.5338952, 651.43678021, 571.14670341, 
            648.51999837 };


        // y = ax + b. To split two hands
        const double paramA = 2.903950;
        const double paramB = -2051.836766;

        class Point
        {
            public double x, y;

            public Point(double _x, double _y)
            {
                x = _x;
                y = _y;
            }
        }

        public WordPredictor()
        {
            loadStatus = false;
            freqDict = new Dictionary<string, int>();
            codeSet = new Dictionary<string, List<string>>();
            vecParams = new Dictionary<string, VectorParameter>();
        }

        public void initialize()
        {
            loadCorpus();
            loadKeyboardVectors();

            loadStatus = true;
        }

        private string encodeWord(string word)
        {
            string code = "";
            foreach (char ch in word.ToLower())
            {
                int chNo = (int)ch - (int)'a' + 1;
                if (chNo >= 1 && chNo < handCode.Length)
                {
                    code += handCode[chNo];
                }
            }
            return code;
        }

        private void loadCorpus()
        {
            string jsonWordFreqName = "Resources/wordFreq.json";
            string jsonCodeSetName = "Resources/codeSet.json";
            if (!File.Exists(jsonWordFreqName) || !File.Exists(jsonCodeSetName))
            {
                // Load from txt fileL: 160000 word corpus.
                string fName = "Resources/en_US_wordlist.combined";
                string[] lines = File.ReadAllLines(fName);

                for (var i = 1; i < lines.Length; ++i)
                {
                    string[] wordParams = lines[i].Split(',');
                    if (wordParams.Length == 4)
                    {
                        string word = wordParams[0].ToLower().Split('=')[1];
                        int freq = Convert.ToInt32(wordParams[3].Split('=')[1]);

                        if (freqDict.ContainsKey(word))
                        {
                            freqDict[word] += freq;
                        }
                        else
                        {
                            freqDict[word] = freq;
                        }

                        string code = encodeWord(word);
                        if (!codeSet.ContainsKey(code))
                        {
                            codeSet[code] = new List<string>();
                        }
                        codeSet[code].Add(word);
                    }
                }

                // MacKenzie
                fName = "Resources/TaskText_All.txt";
                lines = File.ReadAllLines(fName);

                foreach (string line in lines)
                {
                    if (line.Length > 1)
                    {
                        string[] words = line.ToLower().Split(' ');

                        foreach (string word in words)
                        {
                            if (freqDict.ContainsKey(word))
                            {
                                ++freqDict[word];
                            }
                            else
                            {
                                freqDict[word] = 1;
                            }

                            string code = encodeWord(word);
                            if (!codeSet.ContainsKey(code))
                            {
                                codeSet[code] = new List<string>();
                            }
                            codeSet[code].Add(word);
                        }
                    }
                }

                string jsonWord = JsonConvert.SerializeObject(freqDict, Formatting.Indented);
                //Console.WriteLine(jsonWord);
                File.WriteAllText(jsonWordFreqName, jsonWord);

                string jsonCode = JsonConvert.SerializeObject(codeSet, Formatting.Indented);
                //Console.WriteLine(jsonCode);
                File.WriteAllText(jsonCodeSetName, jsonCode);

                Console.WriteLine("Corpus JSON Object Saved.");
            }
            else
            {
                // Read from JSON file
                string jsonWord = File.ReadAllText(jsonWordFreqName);
                freqDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonWord);
                //Console.WriteLine(freqDict);

                string jsonCode = File.ReadAllText(jsonCodeSetName);
                codeSet = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonCode);
                //Console.WriteLine(codeSet);

                Console.WriteLine("Corpus JSON Object Load");
            }
        }

        private void loadKeyboardVectors()
        {
            string jsonKbdVecName =  "Resources/kbdVec.json";
            if (!File.Exists(jsonKbdVecName))
            {
                // Load from csv file
                string fName = "Resources/16users_kbd_vec_v1.2.csv";
                string[] words = File.ReadAllLines(fName);

                for (var i = 1; i < words.Length; ++i)
                {
                    string[] wordParams = words[i].Split(',');
                    if (wordParams.Length == 13)
                    {
                        string charPair = wordParams[0].Trim('\'');
                        double vecX = Convert.ToDouble(wordParams[3]);
                        double vecY = Convert.ToDouble(wordParams[4]);
                        double vecLen = Convert.ToDouble(wordParams[5]);
                        double rad1 = Convert.ToDouble(wordParams[6]);
                        double rad2 = Convert.ToDouble(wordParams[7]);

                        vecParams[charPair] = new VectorParameter(vecX, vecY, vecLen, rad1, rad2);
                    }
                }

                // Fill out other items
                for (var i = 0; i < 26; ++i)
                {
                    for (var j = 0; j < 26; ++j)
                    {
                        string charPair = ((char)((int)'a' + i)).ToString() + ((char)((int)'a' + j)).ToString();
                        if (!vecParams.ContainsKey(charPair))
                        {
                            Console.WriteLine("Add: " + charPair);
                            if (i == j)
                            {
                                vecParams[charPair] = new VectorParameter(0, 0, 0, 0, 0);
                            }
                            else
                            {
                                double vecX = userPosX[j] - userPosX[i];
                                double vecY = userPosY[j] - userPosY[i];

                                vecParams[charPair] = new VectorParameter(vecX, vecY);
                            }
                        }
                    }
                }

                string jsonVecParams = JsonConvert.SerializeObject(vecParams, Formatting.Indented);
                File.WriteAllText(jsonKbdVecName, jsonVecParams);

                Console.WriteLine("KbdVec JSON Object Saved.");
            }
            else
            {
                // Read from JSON file
                string jsonVecParams = File.ReadAllText(jsonKbdVecName);
                vecParams = JsonConvert.DeserializeObject<Dictionary<string, VectorParameter>>(jsonVecParams);

                Console.WriteLine("KbdVec JSON Object Load");
            }
        }

        private List<string> calcUserCodes(double[] pntListX, double[] pntListY)
        {
            //foreach (double xx in pntListX) Console.Write(xx);
            //Console.Write(", ");
            //foreach (double yy in pntListY) Console.Write(yy);
            //Console.WriteLine(";");
            
            List<string> codes = new List<string>();
            List<string> suffixCodes = new List<string>();
            if (pntListX.Length > 1)
            {
                suffixCodes = calcUserCodes(pntListX.Skip<double>(1).ToArray(), pntListY.Skip<double>(1).ToArray());
            }
            else
            {
                suffixCodes.Add("");
            }

            double y = paramA * pntListX[0] + paramB;
            if (y < pntListY[0] - 100)
            {
                // Left
                foreach (string suffix in suffixCodes)
                {
                    codes.Add("0" + suffix);
                }
            }
            else if (y > pntListY[0] + 100)
            {
                // Right
                foreach (string suffix in suffixCodes)
                {
                    codes.Add("1" + suffix);
                }
            }
            else
            {
                // Middle, Try both
                foreach (string suffix in suffixCodes)
                {
                    codes.Add("0" + suffix);
                    codes.Add("1" + suffix);
                }
            }

            return codes;
        }

        private static int MyCompare(KeyValuePair<string, double> kvp1, KeyValuePair<string, double> kvp2)
        {
            return kvp1.Value.CompareTo(kvp2.Value);
        }

        public List<KeyValuePair<string, double>> predict(double[] pntListX, double[] pntListY)
        {
            // Get user codes
            List<KeyValuePair<string, double>> probWords = new List<KeyValuePair<string,double>>();

            List<string> userCodes = calcUserCodes(pntListX, pntListY);
            Console.WriteLine(userCodes[0]);

            //List<int> myPntIdL = new List<int>();
            //List<int> myPntIdR = new List<int>();
            List<Point> myPntL = new List<Point>();
            List<Point> myPntR = new List<Point>();
            List<VectorParameter> myVecL = new List<VectorParameter>();
            List<VectorParameter> myVecR = new List<VectorParameter>();

            List<Point> candWordPntL = new List<Point>();
            List<Point> candWordPntR = new List<Point>();
            List<VectorParameter> candWordVecL = new List<VectorParameter>();
            List<VectorParameter> candWordVecR = new List<VectorParameter>();

            foreach (string userCode in userCodes)
            {
                if (codeSet.ContainsKey(userCode))
                {
                    //myPntIdL.Clear();
                    //myPntIdR.Clear();
                    myPntL.Clear();
                    myPntR.Clear();
                    myVecL.Clear();
                    myVecR.Clear();

                    // Generate vectors for this code
                    for (var i = 0; i < userCode.Length; ++i)
                    {
                        if (userCode[i] == '0')
                        {
                            if (myPntL.Count > 0)
                            {
                                double vecX = pntListX[i] - myPntL.Last().x;
                                double vecY = pntListY[i] - myPntL.Last().y;

                                myVecL.Add(new VectorParameter(vecX, vecY));
                            }
                            myPntL.Add(new Point(pntListX[i], pntListY[i]));
                        }
                        else
                        {
                            if (myPntR.Count > 0)
                            {
                                double vecX = pntListX[i] - myPntR.Last().x;
                                double vecY = pntListY[i] - myPntR.Last().y;

                                myVecR.Add(new VectorParameter(vecX, vecY));
                            }
                            myPntR.Add(new Point(pntListX[i], pntListY[i]));
                        }
                    }

                    // Check each candidate words

                    List<string> selWords = codeSet[userCode];
                    foreach (string candWord in selWords)
                    {
                        candWordPntL.Clear();
                        candWordPntR.Clear();
                        candWordVecL.Clear();
                        candWordVecR.Clear();

                        char lastLeft = ' ', lastRight = ' ';

                        for (var i = 0; i < candWord.Length; ++i)
                        {
                            char currChar = candWord[i];
                            if (currChar >= 'a' && currChar <= 'z')
                            {
                                int currCharNo = currChar - 'a';
                                if (handCode[currCharNo + 1][0] == '0')
                                {
                                    // Left
                                    if (candWordPntL.Count > 0)
                                    {
                                        candWordVecL.Add(vecParams[lastLeft.ToString() + currChar.ToString()]);
                                    }
                                    lastLeft = currChar;
                                    candWordPntL.Add(new Point(letterPosX[currCharNo], letterPosY[currCharNo]));
                                }
                                else
                                {
                                    // Right
                                    if (candWordPntR.Count > 0)
                                    {
                                        candWordVecR.Add(vecParams[lastRight.ToString() + currChar.ToString()]);
                                    }
                                    lastRight = currChar;
                                    candWordPntR.Add(new Point(letterPosX[currCharNo], letterPosY[currCharNo]));
                                }
                            }
                        }

                        // Calculate distance
                        double distance = 0.0;

                        for (var i = 0; i < myVecL.Count; ++i)
                        {
                            VectorParameter mVec = myVecL[i];
                            VectorParameter cwVec = candWordVecL[i];
                            distance += mVec.getDistance(cwVec);
                        }

                        if (myPntL.Count == 1)
                        {
                            VectorParameter pseudoMVec = new VectorParameter(myPntL[0].x, myPntL[0].y, 0, 0, 0);
                            VectorParameter pseudoCWVec = new VectorParameter(candWordPntL[0].x, candWordPntL[0].y, 0, 0, 0);
                            distance += pseudoMVec.getDistance(pseudoCWVec) * Math.Exp(-candWord.Length / 10.0);

                            if (myPntR.Count > 0)
                            {
                                VectorParameter mVec = new VectorParameter(myPntR[0].x - myPntL[0].x, myPntR[0].y - myPntL[0].y);
                                VectorParameter cwVec = new VectorParameter(candWordPntR[0].x - candWordPntL[0].x,
                                    candWordPntR[0].y - candWordPntL[0].y);
                                distance += mVec.getDistance(cwVec) * Math.Exp(-candWord.Length / 10.0);
                            }
                        }

                        for (var i = 0; i < myVecR.Count; ++i)
                        {
                            VectorParameter mVec = myVecR[i];
                            VectorParameter cwVec = candWordVecR[i];
                            distance += mVec.getDistance(cwVec);
                        }

                        if (myPntR.Count == 1)
                        {
                            VectorParameter pseudoMVec = new VectorParameter(myPntR[0].x, myPntR[0].y, 0, 0, 0);
                            VectorParameter pseudoCWVec = new VectorParameter(candWordPntR[0].x, candWordPntR[0].y, 0, 0, 0);
                            distance += pseudoMVec.getDistance(pseudoCWVec) * Math.Exp(-candWord.Length / 10.0);

                            if (myPntL.Count > 0)
                            {
                                VectorParameter mVec = new VectorParameter(myPntL[0].x - myPntR[0].x, myPntL[0].y - myPntR[0].y);
                                VectorParameter cwVec = new VectorParameter(candWordPntL[0].x - candWordPntR[0].x,
                                    candWordPntL[0].y - candWordPntR[0].y);
                                distance += mVec.getDistance(cwVec) * Math.Exp(-candWord.Length / 10.0);
                            }
                        }

                        probWords.Add(new KeyValuePair<string, double>(candWord, distance));

                    }
                }
            }

            probWords.Sort(MyCompare);

            //probWords.Add("Hello World!");
            return probWords;
        }
    }
}
