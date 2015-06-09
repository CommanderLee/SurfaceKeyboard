using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Newtonsoft.Json;

namespace SurfaceKeyboard
{
    enum PredictionMode { RelativeMode, AbsoluteMode };

    class WordPredictor
    {
        public bool                                 loadStatus;
        public Dictionary<string, int>              freqDict;
        public Dictionary<string, List<string>>     codeSet;
        //public Dictionary<int, List<string>> lenSet;
        public Trie                                 trieFreq;
        private double[]                            currPntX, currPntY;

        public Dictionary<string, VectorParameter>  vecParams;
        public Dictionary<char, PointParameter>     pntParams;

        private int                                 wordGramLen = 5;

        private double                              freqMax, freqMin, freqAvg;

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
        //static double[] userPosX = new double[] { 718.57701378, 909.28646996, 833.47336852, 813.78691024, 798.0410731, 853.90674048, 891.43037533, 
        //    936.84420906, 1005.498639, 970.26316325, 1017.0088152, 1057.8388884, 992.81661424, 953.96573418, 1046.8064112, 1091.973913, 
        //    709.51111111, 840.63848851, 767.88189101, 880.79090006, 962.8251284, 875.36052006, 755.24576271, 789.71264368, 926.77008268, 
        //    747.06666667 };
        //static double[] userPosY = new double[] { 608.76643043, 652.66137654, 649.99647352, 607.69583868, 567.42299606, 611.19359811, 610.85930762, 
        //    613.12794869, 566.97748005, 618.57894951, 605.59700376, 604.27777744, 652.05537706, 654.45690642, 565.00218248, 565.54347746, 
        //    574.67778049, 571.56836324, 609.71508114, 574.24703991, 571.10992157, 652.48759038, 570.5338952, 651.43678021, 571.14670341, 
        //    648.51999837 };
        //double[] userPosX, userPosY;
        //double[] userStdX, userStdY;
        //double[] userVarX, userVarY;
        //double[] userCovXY, userCorrXY;



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
            //lenSet = new Dictionary<int, List<string>>();
            // Root of the trie struct
            trieFreq = new Trie('$');

            vecParams = new Dictionary<string, VectorParameter>();
            pntParams = new Dictionary<char, PointParameter>();
        }

        public void initialize()
        {
            loadCorpus();
            loadKeyboardPoints();
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
            string jsonLenSetName = "Resources/lenSet.json";
            if (!File.Exists(jsonWordFreqName) || !File.Exists(jsonCodeSetName) || !File.Exists(jsonLenSetName))
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
                        
                        // Only keep alphabets
                        string tmpWord = "";
                        foreach (char c in word)
                        {
                            if (c >= 'a' && c <= 'z')
                            {
                                tmpWord += c.ToString();
                            }
                        }
                        word = tmpWord;

                        if (freqDict.ContainsKey(word))
                        {
                            freqDict[word] += freq + 1;
                        }
                        else
                        {
                            freqDict[word] = freq + 1;
                        }

                        string code = encodeWord(word);
                        if (!codeSet.ContainsKey(code))
                        {
                            codeSet[code] = new List<string>();
                        }
                        if (!codeSet[code].Contains(word))
                        {
                            codeSet[code].Add(word);
                        }

                        //int len = word.Length;
                        //if (!lenSet.ContainsKey(len))
                        //{
                        //    lenSet[len] = new List<string>();
                        //}
                        //if (!lenSet[len].Contains(word))
                        //{
                        //    lenSet[len].Add(word);
                        //}
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

                            if (!codeSet[code].Contains(word))
                            {
                                codeSet[code].Add(word);
                                Console.WriteLine("New Word: " + word + "; Code: " + code + ";Num: " + codeSet[code].Count);
                            }

                            //int len = word.Length;
                            //if (!lenSet.ContainsKey(len))
                            //{
                            //    lenSet[len] = new List<string>();
                            //}

                            //if (!lenSet[len].Contains(word))
                            //{
                            //    lenSet[len].Add(word);
                            //    Console.WriteLine("New Word: " + word + "; Len: " + len + ";Num: " + lenSet[len].Count);
                            //}
                        }
                    }
                }

                string jsonWord = JsonConvert.SerializeObject(freqDict, Formatting.Indented);
                //Console.WriteLine(jsonWord);
                File.WriteAllText(jsonWordFreqName, jsonWord);

                string jsonCode = JsonConvert.SerializeObject(codeSet, Formatting.Indented);
                //Console.WriteLine(jsonCode);
                File.WriteAllText(jsonCodeSetName, jsonCode);

                //string jsonLen = JsonConvert.SerializeObject(lenSet, Formatting.Indented);
                //File.WriteAllText(jsonLenSetName, jsonLen);

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

                //string jsonLen = File.ReadAllText(jsonLenSetName);
                //lenSet = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(jsonLen);

                Console.WriteLine("Corpus JSON Object Load");
            }
            freqMax = freqDict.Values.Max();
            freqMin = freqDict.Values.Min();
            freqAvg = freqDict.Values.ToList().Average();
            Console.WriteLine("[" + freqMin + "-" + freqMax + "]" + "Avg:" + freqAvg);

            // Load Trie from word-freq dictionary
            foreach (KeyValuePair<string, int> kvPair in freqDict)
            {
                string word = kvPair.Key;
                int freq = kvPair.Value;
                if (word.Length < wordGramLen)
                {
                    trieFreq.addChild(word, freq);
                }
                else
                {
                    for (var i = 0; i + wordGramLen <= word.Length; ++i)
                    {
                        trieFreq.addChild(word.Substring(i, wordGramLen), freq);
                    }
                }
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
                        char charA = (char)((int)'a' + i);
                        char charB = (char)((int)'a' + j);
                        string charPair = charA.ToString() + charB.ToString();
                        if (!vecParams.ContainsKey(charPair))
                        {
                            Console.WriteLine("Add: " + charPair);
                            if (i == j)
                            {
                                vecParams[charPair] = new VectorParameter(0, 0, 0, 0, 0);
                            }
                            else
                            {
                                double vecX = pntParams[charB].userPosX - pntParams[charA].userPosX;
                                double vecY = pntParams[charB].userPosY - pntParams[charA].userPosY;

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

        private void loadKeyboardPoints()
        {
            string jsonKbdPntName = "Resources/kbdPnt.json";
            if (!File.Exists(jsonKbdPntName))
            {
                // Load from csv file
                string fName = "Resources/pointPosResult-16users-v2.5-merged.txt";
                string[] lines = File.ReadAllLines(fName);

                for (var i = 1; i < lines.Length; ++i)
                {
                    string[] wordParams = lines[i].Split(',');
                    if (wordParams.Length == 11)
                    {
                        char uChar = wordParams[0][1];
                        double posX = Convert.ToDouble(wordParams[2]);
                        double posY = Convert.ToDouble(wordParams[3]);
                        double stdX = Convert.ToDouble(wordParams[4]);
                        double stdY = Convert.ToDouble(wordParams[5]);
                        double varX = Convert.ToDouble(wordParams[6]);
                        double varY = Convert.ToDouble(wordParams[7]);
                        double covXY = Convert.ToDouble(wordParams[9]);
                        double corrXY = Convert.ToDouble(wordParams[10]);


                        pntParams[uChar] = new PointParameter(posX, posY, stdX, stdY,
                            varX, varY, covXY, corrXY);

                    }
                }

                string jsonPntParams = JsonConvert.SerializeObject(pntParams, Formatting.Indented);
                File.WriteAllText(jsonKbdPntName, jsonPntParams);

                Console.WriteLine("KbdPnt JSON Object Saved.");
            }
            else
            {
                // Read from JSON file
                string jsonPntParams = File.ReadAllText(jsonKbdPntName);
                pntParams = JsonConvert.DeserializeObject<Dictionary<char, PointParameter>>(jsonPntParams);

                Console.WriteLine("KbdPnt JSON Object Load");
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

        private static int MyCompareUp(KeyValuePair<string, double> kvp1, KeyValuePair<string, double> kvp2)
        {
            return kvp1.Value.CompareTo(kvp2.Value);
        }

        private static int MyCompareDownn(KeyValuePair<string, double> kvp1, KeyValuePair<string, double> kvp2)
        {
            return kvp2.Value.CompareTo(kvp1.Value);
        }

        public List<KeyValuePair<string, double>> predict(double[] pntListX, double[] pntListY, PredictionMode predMode)
        {
            // Get user codes
            List<KeyValuePair<string, double>> probWords = new List<KeyValuePair<string,double>>();

            if (predMode == PredictionMode.RelativeMode)
            {
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

                            // distance *= freqDict[candWord]; // / Math.Max(1, 2 - distance / freqAvg);

                            probWords.Add(new KeyValuePair<string, double>(candWord, distance));

                        }
                    }
                }
                // Smaller distance is better. Sort Up.
                probWords.Sort(MyCompareUp);
            }
            else
            {
                //List<string> selWords = lenSet[pntListX.Count()];
                //foreach (string canWord in selWords)

                currPntX = pntListX;
                currPntY = pntListY;
                probWords = dfs(1.0, 0, "");

                // Bigger probability is better. Sort Down.
                probWords.Sort(MyCompareDownn);
            }
            
            return probWords;
        }

        private static int MyCompareProb(KeyValuePair<char, double> kvp1, KeyValuePair<char, double> kvp2)
        {
            return kvp2.Value.CompareTo(kvp1.Value);
        }

        /// <summary>
        /// Depth First Search. Find possible letter sequences
        /// </summary>
        /// <param name="prob"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private List<KeyValuePair<string, double>> dfs(double prob, int index, string seq)
        {
            List<KeyValuePair<string, double>> ret = new List<KeyValuePair<string, double>>();

            // Get candidates
            List<KeyValuePair<char, double>> tmpProbPair = new List<KeyValuePair<char, double>>();

            for (var i = 0; i < 26; ++i)
            {
                char c = (char)('a' + i);
                PointParameter pp = pntParams[c];
                string tmpSeq = seq + c.ToString();

                // Goal: argmax P(letter seq) * P(point pos | letter seq)
                // P(letter seq) = P(L1) * P(L2 | L1) * ... * P(Ln | Ln-k..Ln-1)
                // Calculate P(point pos | letter seq) = P1 * P2 * ... * Pn
                double currProb = biVarGaussianDistribution(currPntX[index], currPntY[index],
                    pp.userPosX, pp.userPosY, pp.userStdX, pp.userStdY, pp.userCorrXY);
                int startIndex = Math.Max(0, index - wordGramLen + 1);
                if (index - startIndex >= 1)
                {
                    currProb *= (double)(trieFreq.findChild(tmpSeq.Substring(startIndex, index - startIndex + 1))) /
                        trieFreq.findChild(tmpSeq.Substring(startIndex, index - startIndex));
                }

                tmpProbPair.Add(new KeyValuePair<char, double>(c, currProb));
            }

            tmpProbPair.Sort(MyCompareProb);

            if (index == currPntX.Length - 1)
            {
                // End.
                for (var i = 0; i < 5; ++i)
                {
                    ret.Add(new KeyValuePair<string, double>(seq + tmpProbPair[i].Key.ToString(), prob * tmpProbPair[i].Value));
                }
            }
            else
            {
                // End.
                for (var i = 0; i < 5; ++i)
                {
                    ret.AddRange(dfs(prob * tmpProbPair[i].Value, index + 1, seq + tmpProbPair[i].Key.ToString()));
                }
            }

            return ret;
        }

        private double biVarGaussianDistribution(double x, double y, double muX, double muY,
            double sigmaX, double sigmaY, double rho)
        {
            return 1 / (2 * Math.PI * sigmaX * sigmaY * Math.Sqrt(1 - rho*rho)) 
                * Math.Exp(- 1 / (2 * (1 - rho*rho)) * (Math.Pow(x - muX, 2) / Math.Pow(sigmaX, 2) 
                - 2 * rho * (x - muX) * (y - muY) / (sigmaX * sigmaY) 
                + Math.Pow(y - muY, 2) / Math.Pow(sigmaY, 2)));
        }
    }
}
