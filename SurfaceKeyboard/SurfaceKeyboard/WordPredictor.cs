using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Newtonsoft.Json;

namespace SurfaceKeyboard
{
    enum PredictionMode { RelativeMode, AbsoluteMode, DirectMode };

    class WordPredictor
    {
        public bool                                 loadStatus;
        public Dictionary<string, int>              freqDict;
        public Dictionary<string, List<string>>     codeSet;
        public Dictionary<int, List<string>>        lenSet;

        public Trie                                 trieFreq;
        //private double[]                            currPntX, currPntY;

        public Dictionary<string, VectorParameter>  vecParams;
        public Dictionary<char, PointParameter>     pntParams;

        private int                                 wordGramLen = 5;

        private double                              freqMax, freqMin, freqAvg;

        List<KeyValuePair<string, double>>          probWords;
        private int                                 searchNum = 10;
        // Set up with a large length
        private int                                 lastSentenceLen = 100;

        // Key-Size
        static double                               keySizeX = 39.0;
        static double                               keySizeY = 42.0;

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
        static double[] letterPosY = new double[] { 616.4, 660.4, 660.4, 616.4, 572.4, 616.4, 616.4, 616.4, 572.4, 616.4, 616.4, 616.4, 
            660.4, 660.4, 572.4, 572.4, 572.4, 572.4, 616.4, 572.4, 572.4, 660.4, 572.4, 660.4, 572.4, 660.4 };

        const int SPACE_TOP = 690;
        const int SPACE_LEFT = 760;
        const int SPACE_RIGHT = 1110;

        const int TYPE_LEFT = 703;
        const int TYPE_RIGHT = 1284;
        const int TYPE_TOP = 507;

        Point BACKSPACE_TOP_LEFT = new Point(1214, 507);
        Point BACKSPACE_BOTTOM_RIGHT = new Point(1284, 553);

        Point DEL_TOP_LEFT = new Point(1234, 553);
        Point DEL_BOTTOM_RIGHT = new Point(1284, 597);

        Point ENTER_TOP_LEFT = new Point(1173, 597);
        Point ENTER_BOTTOM_RIGHT = new Point(1281, 642);

        // Calculated from user data
        // mean(vecX) = a * KbdVecX + b * KbdVecX + c
        const double xParamA = 1.0226471;
        const double xParamB = -0.090838;
        const double xParamC = 0.1231523;

        // std(vecX) : d, e, f
        const double xParamD = 0.0493039;
        const double xParamE = -0.004056;
        const double xParamF = 10.135981;

        // mean(vecY) and std(vecY): -88, -44, 0, 44, 88(-2, -1, 0, 1, 2 lines)
        static double[] meanVecY = new double[] { -83.58715, -42.39778, -0.52703, 41.879518, 85.007872};
        static double[] stdVecY = new double[] { 13.664421, 11.643536, 10.7334, 12.16603, 12.680535};

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
            lenSet = new Dictionary<int, List<string>>();

            // Root of the trie struct
            trieFreq = new Trie('$');

            vecParams = new Dictionary<string, VectorParameter>();
            pntParams = new Dictionary<char, PointParameter>();

            probWords = new List<KeyValuePair<string, double>>();
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
                int maxLen = 50000;

                // Load from Android Source. txt fileL: 160000 word corpus.
                string fName = "Resources/en_US_wordlist.combined";
                string[] lines = File.ReadAllLines(fName);
                Dictionary<string, int> tempFreqDict = new Dictionary<string, int>();
                Dictionary<string, List<string>> tempCodeSet = new Dictionary<string,List<string>>();
                Dictionary<int, List<string>> tempLenSet = new Dictionary<int, List<string>>();
                
                int selectLen = Math.Min(maxLen, lines.Length);
                for (var i = 1; i < selectLen; ++i)
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

                        if (tempFreqDict.ContainsKey(word))
                        {
                            tempFreqDict[word] += freq + 1;
                        }
                        else
                        {
                            tempFreqDict[word] = freq + 1;
                        }

                        string code = encodeWord(word);
                        if (!tempCodeSet.ContainsKey(code))
                        {
                            tempCodeSet[code] = new List<string>();
                        }
                        if (!tempCodeSet[code].Contains(word))
                        {
                            tempCodeSet[code].Add(word);
                        }

                        int len = word.Length;
                        if (!tempLenSet.ContainsKey(len))
                        {
                            tempLenSet[len] = new List<string>();
                        }
                        if (!tempLenSet[len].Contains(word))
                        {
                            tempLenSet[len].Add(word);
                        }

                        //if (freqDict.ContainsKey(word))
                        //{
                        //    freqDict[word] += freq + 1;
                        //}
                        //else
                        //{
                        //    freqDict[word] = freq + 1;
                        //}

                        //string code = encodeWord(word);
                        //if (!codeSet.ContainsKey(code))
                        //{
                        //    codeSet[code] = new List<string>();
                        //}
                        //if (!codeSet[code].Contains(word))
                        //{
                        //    codeSet[code].Add(word);
                        //}

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

                // Load from ANC Written Corpus.
                fName = "Resources/ANC-written-count.txt";
                lines = File.ReadAllLines(fName);

                selectLen = Math.Min(maxLen, lines.Length);
                for (var i = 0; i < selectLen; ++i)
                {
                    string[] wordParams = lines[i].Split('\t');
                    if (wordParams.Length == 4)
                    {
                        string word = wordParams[0].ToLower();
                        int freq = Convert.ToInt32(wordParams[3]);

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

                        // Get Intersection
                        if (tempFreqDict.ContainsKey(word))
                        {
                            if (freqDict.ContainsKey(word))
                                freqDict[word] += freq;
                            else
                                freqDict[word] = freq;// tempFreqDict[word];

                            string code = encodeWord(word);
                            if (!codeSet.ContainsKey(code))
                            {
                                codeSet[code] = new List<string>();
                            }
                            if (!codeSet[code].Contains(word))
                            {
                                codeSet[code].Add(word);
                            }

                            int len = word.Length;
                            if (!lenSet.ContainsKey(len))
                            {
                                lenSet[len] = new List<string>();
                            }
                            if (!lenSet[len].Contains(word))
                            {
                                lenSet[len].Add(word);
                            }
                        }

                        //if (freqDict.ContainsKey(word))
                        //{
                        //    freqDict[word] += freq + 1;
                        //    ++commonWords;
                        //}
                        //else
                        //{
                        //    freqDict[word] = freq + 1;
                        //}

                        //string code = encodeWord(word);
                        //if (!codeSet.ContainsKey(code))
                        //{
                        //    codeSet[code] = new List<string>();
                        //}
                        //if (!codeSet[code].Contains(word))
                        //{
                        //    codeSet[code].Add(word);
                        //}

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

                // Check Testing set
                fName = "Resources/TaskText_Mixed_40.txt";
                lines = File.ReadAllLines(fName);
                string existingTexts = "";

                foreach (string line in lines)
                {
                    if (line.Length > 1)
                    {
                        bool allExist = true;
                        int minFreq = 1000;

                        string[] words = line.ToLower().Split(' ');

                        foreach (string word in words)
                        {
                            if (freqDict.ContainsKey(word))
                            {
                                if (freqDict[word] < minFreq)
                                    minFreq = freqDict[word];
                            }
                            else
                            {
                                allExist = false;
                            }
                        }

                        if (allExist)
                        {
                            Console.WriteLine("MinFreq:" + minFreq);
                            existingTexts += line + "\n";
                        }
                        else
                        {
                            Console.WriteLine("Not Exist.");
                        }
                    }
                }
                File.WriteAllText("Resources/TaskTexts_Modified.txt", existingTexts);

                // MacKenzie
                fName = "Resources/TaskText_All.txt";
                lines = File.ReadAllLines(fName);
                existingTexts ="";

                foreach (string line in lines)
                {
                    if (line.Length > 1)
                    {
                        bool allExist = true;

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
                                Console.WriteLine("New Word: " + word);
                                allExist = false;
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

                            int len = word.Length;
                            if (!lenSet.ContainsKey(len))
                            {
                                lenSet[len] = new List<string>();
                            }
                            if (!lenSet[len].Contains(word))
                            {
                                lenSet[len].Add(word);
                            }
                        }

                        if (allExist)
                            existingTexts += line + "\n";
                    }
                }
                File.WriteAllText("Resources/SelectedTexts.txt", existingTexts);

                string jsonWord = JsonConvert.SerializeObject(freqDict, Formatting.Indented);
                //Console.WriteLine(jsonWord);
                File.WriteAllText(jsonWordFreqName, jsonWord);

                string jsonCode = JsonConvert.SerializeObject(codeSet, Formatting.Indented);
                //Console.WriteLine(jsonCode);
                File.WriteAllText(jsonCodeSetName, jsonCode);

                string jsonLen = JsonConvert.SerializeObject(lenSet, Formatting.Indented);
                File.WriteAllText(jsonLenSetName, jsonLen);

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

                string jsonLen = File.ReadAllText(jsonLenSetName);
                lenSet = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(jsonLen);
                //Console.WriteLine(codeSet);

                Console.WriteLine("Corpus JSON Object Load");
            }
            freqMax = freqDict.Values.Max();
            freqMin = freqDict.Values.Min();
            freqAvg = freqDict.Values.ToList().Average();
            Console.WriteLine("[" + freqMin + "-" + freqMax + "]" + "Avg:" + freqAvg);

            // Load Trie from word-freq dictionary
            //foreach (KeyValuePair<string, int> kvPair in freqDict)
            //{
            //    string word = kvPair.Key;
            //    int freq = kvPair.Value;
            //    if (word.Length < wordGramLen)
            //    {
            //        trieFreq.addChild(word, freq);
            //    }
            //    else
            //    {
            //        for (var i = 0; i + wordGramLen <= word.Length; ++i)
            //        {
            //            trieFreq.addChild(word.Substring(i, wordGramLen), freq);
            //        }
            //    }
            //}
        }

        private void loadKeyboardVectors()
        {
            string jsonKbdVecName =  "Resources/kbdVec.json";
            if (!File.Exists(jsonKbdVecName))
            {
                // Load from csv file
                string fName = "Resources/pointPairResult-16users-v2.5-merged.txt";
                string[] words = File.ReadAllLines(fName);

                double sumSVX = 0, sumSVY = 0, sumSVL = 0, sumRD = 0;
                int cnt = 0;
                for (var i = 1; i < words.Length; ++i)
                {
                    string[] wordParams = words[i].Split(',');
                    if (wordParams.Length == 12)
                    {
                        ++cnt;

                        string charPair = wordParams[0].Trim('\'');
                        double vecX = Convert.ToDouble(wordParams[3]);
                        double vecY = Convert.ToDouble(wordParams[4]);
                        double vecLen = Convert.ToDouble(wordParams[5]);
                        double rad1 = Convert.ToDouble(wordParams[6]);
                        double rad2 = Convert.ToDouble(wordParams[7]);

                        double stdVecX = Convert.ToDouble(wordParams[8]);
                        double stdVecY = Convert.ToDouble(wordParams[9]);
                        double stdVecLen = Convert.ToDouble(wordParams[10]);
                        double radDist = Convert.ToDouble(wordParams[11]);
                        //Console.WriteLine(words[i] + "\n    " + vecX + ", " + stdVecX);

                        vecParams[charPair] = new VectorParameter(vecX, vecY, vecLen, rad1, rad2, 
                            stdVecX, stdVecY, stdVecLen, radDist);

                        sumSVX += stdVecX;
                        sumSVY += stdVecY;
                        sumSVL += stdVecLen;
                        sumRD += radDist;
                    }
                }

                // Use general(mean) value for those non-existing vectors
                double genSVX = sumSVX / cnt;
                double genSVY = sumSVY / cnt;
                double genSVL = sumSVL / cnt;
                double genRD = sumRD / cnt;

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
                                vecParams[charPair] = new VectorParameter(0, 0, -1, -1, -1, genSVX, genSVY, genSVL, genRD);
                            }
                            else
                            {
                                double vecX = pntParams[charB].userPosX - pntParams[charA].userPosX;
                                double vecY = pntParams[charB].userPosY - pntParams[charA].userPosY;

                                vecParams[charPair] = new VectorParameter(vecX, vecY, -1, -1, -1, genSVX, genSVY, genSVL, genRD);
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

        public bool isValidArea(double x, double y)
        {
            return (x >= TYPE_LEFT && x <= TYPE_RIGHT && y >= TYPE_TOP);
        }

        public bool isSpacebar(double x, double y)
        {
            return (x >= SPACE_LEFT && x <= SPACE_RIGHT && y >= SPACE_TOP);
        }

        public bool isBackspace(double x, double y)
        {
            return (x >= BACKSPACE_TOP_LEFT.x && x <= BACKSPACE_BOTTOM_RIGHT.x && y >= BACKSPACE_TOP_LEFT.y && y <= BACKSPACE_BOTTOM_RIGHT.y);
        }

        public bool isDel(double x, double y)
        {
            return (x >= DEL_TOP_LEFT.x && x <= DEL_BOTTOM_RIGHT.x && y >= DEL_TOP_LEFT.y && y <= DEL_BOTTOM_RIGHT.y);
        }

        public bool isEnter(double x, double y)
        {
            return (x >= ENTER_TOP_LEFT.x && x <= ENTER_BOTTOM_RIGHT.x && y >= ENTER_TOP_LEFT.y && y <= ENTER_BOTTOM_RIGHT.y);
        }

        public List<KeyValuePair<string, double>> predict(double[] pntListX, double[] pntListY, PredictionMode predMode)
        {
            // Get user codes
            //List<KeyValuePair<string, double>> probWords = new List<KeyValuePair<string,double>>();

            if (predMode == PredictionMode.RelativeMode)
            {
                probWords.Clear();

                List<string> userCodes = calcUserCodes(pntListX, pntListY);
                Console.WriteLine(userCodes[0]);

                //List<int> myPntIdL = new List<int>();
                //List<int> myPntIdR = new List<int>();
                List<Point> myPntL = new List<Point>();
                List<Point> myPntR = new List<Point>();
                List<Point> myVecL = new List<Point>();
                List<Point> myVecR = new List<Point>();
                //List<VectorParameter> myVecL = new List<VectorParameter>();
                //List<VectorParameter> myVecR = new List<VectorParameter>();

                List<int> candWordPntIdL = new List<int>();
                List<int> candWordPntIdR = new List<int>();
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

                                    myVecL.Add(new Point(vecX, vecY));
                                    //myVecL.Add(new VectorParameter(vecX, vecY));
                                }
                                myPntL.Add(new Point(pntListX[i], pntListY[i]));
                            }
                            else
                            {
                                if (myPntR.Count > 0)
                                {
                                    double vecX = pntListX[i] - myPntR.Last().x;
                                    double vecY = pntListY[i] - myPntR.Last().y;

                                    myVecR.Add(new Point(vecX, vecY));
                                    //myVecR.Add(new VectorParameter(vecX, vecY));
                                }
                                myPntR.Add(new Point(pntListX[i], pntListY[i]));
                            }
                        }

                        // Check each candidate words

                        List<string> selWords = codeSet[userCode];
                        foreach (string candWord in selWords)
                        {
                            // Probability Model
                            double prob = Math.Log(freqDict[candWord]);
                            double log2pi = Math.Log(2 * Math.PI);

                            candWordPntIdL.Clear();
                            candWordPntIdR.Clear();
                            int leftIndex = 0;
                            int rightIndex = 0;
                            for (var i = 0; i < candWord.Length; ++i)
                            {
                                char currChar = candWord[i];
                                if (currChar >= 'a' && currChar <= 'z')
                                {
                                    int currCharNo = currChar - 'a';
                                    if (handCode[currCharNo + 1][0] == '0')
                                    {
                                        // Left Vector
                                        if (candWordPntIdL.Count > 0)
                                        {
                                            // Calculate this vector
                                            double kbdVecX = letterPosX[currCharNo] - letterPosX[candWord[candWordPntIdL.Last()] - 'a'];
                                            double kbdVecY = letterPosY[currCharNo] - letterPosY[candWord[candWordPntIdL.Last()] - 'a'];
                                            
                                            double myX = myVecL[leftIndex].x;
                                            double muX = xParamA * kbdVecX + xParamB * kbdVecY + xParamC;
                                            double sigmaX = xParamD * kbdVecX + xParamE * kbdVecY + xParamF;
                                            prob += VectorParameter.logGaussianDistribution(myX, muX, sigmaX);
                                            //Console.WriteLine("    L1" + candWord + ": " + prob);

                                            double myY = myVecL[leftIndex].y;
                                            double muY = meanVecY[(int)kbdVecY / 40 + 2];
                                            double sigmaY = stdVecY[(int)kbdVecY / 40 + 2];
                                            prob += VectorParameter.logGaussianDistribution(myY, muY, sigmaY);
                                            //Console.WriteLine("    L2" + candWord + ": " + prob);

                                            ++leftIndex;
                                        }

                                        candWordPntIdL.Add(i);
                                    }
                                    else
                                    {
                                        // Right Vector
                                        if (candWordPntIdR.Count > 0)
                                        {
                                            // Calculate this vector
                                            double kbdVecX = letterPosX[currCharNo] - letterPosX[candWord[candWordPntIdR.Last()] - 'a'];
                                            double kbdVecY = letterPosY[currCharNo] - letterPosY[candWord[candWordPntIdR.Last()] - 'a'];

                                            double myX = myVecR[rightIndex].x;
                                            double muX = xParamA * kbdVecX + xParamB * kbdVecY + xParamC;
                                            double sigmaX = xParamD * kbdVecX + xParamE * kbdVecY + xParamF;
                                            prob += VectorParameter.logGaussianDistribution(myX, muX, sigmaX);
                                            //Console.WriteLine("    R1" + candWord + ": " + prob);

                                            double myY = myVecR[rightIndex].y;
                                            double muY = meanVecY[(int)kbdVecY / 40 + 2];
                                            double sigmaY = stdVecY[(int)kbdVecY / 40 + 2];
                                            prob += VectorParameter.logGaussianDistribution(myY, muY, sigmaY);
                                            //Console.WriteLine("    R2" + candWord + ": " + prob);

                                            ++rightIndex;
                                        }

                                        candWordPntIdR.Add(i);
                                    }
                                }
                            }

                            if (candWordPntIdL.Count >= 1)
                            {
                                // Left Point
                                int pos = candWordPntIdL[0];
                                double x = pntListX[pos];
                                double y = pntListY[pos];
                                
                                PointParameter pp = pntParams[candWord[pos]];
                                double muX = pp.userPosX;
                                double muY = pp.userPosY;
                                double sigmaX = pp.userStdX;
                                double sigmaY = pp.userStdY;
                                double rho = pp.userCorrXY;

                                prob += VectorParameter.logBiGaussianDistribution(x, y, muX, muY, sigmaX, sigmaY, rho);
                                //Console.WriteLine("    LP1" + candWord + ": " + prob);

                                //if (candWordPntIdR.Count > 0)
                                //{
                                //    // Left Point - Right Point
                                //    int posR = candWordPntIdR[0];
                                //    double kbdVecX = letterPosX[candWord[posR] - 'a'] - letterPosX[candWord[pos] - 'a'];
                                //    double kbdVecY = letterPosY[candWord[posR] - 'a'] - letterPosY[candWord[pos] - 'a'];

                                //    double myX = pntListX[posR] - x;
                                //    muX = xParamA * kbdVecX + xParamB * kbdVecY + xParamC;
                                //    sigmaX = xParamD * kbdVecX + xParamE * kbdVecY + xParamF;
                                //    //prob += VectorParameter.logGaussianDistribution(myX, muX, sigmaX);

                                //    double myY = pntListX[posR] - y;
                                //    muY = meanVecY[(int)kbdVecY / 40 + 2];
                                //    sigmaY = stdVecY[(int)kbdVecY / 40 + 2];
                                //    //prob += VectorParameter.logGaussianDistribution(myY, muY, sigmaY);
                                //}
                            }

                            if (candWordPntIdR.Count >= 1)
                            {
                                int pos = candWordPntIdR[0];
                                double x = pntListX[pos];
                                double y = pntListY[pos];

                                PointParameter pp = pntParams[candWord[pos]];
                                double muX = pp.userPosX;
                                double muY = pp.userPosY;
                                double sigmaX = pp.userStdX;
                                double sigmaY = pp.userStdY;
                                double rho = pp.userCorrXY;

                                prob += VectorParameter.logBiGaussianDistribution(x, y, muX, muY, sigmaX, sigmaY, rho);
                                //Console.WriteLine("    RP1" + candWord + ": " + prob);

                                //if (candWordPntIdL.Count > 0)
                                //{
                                //    // Right Point - Left Point
                                //    int posL = candWordPntIdL[0];
                                //    double kbdVecX = letterPosX[candWord[posL] - 'a'] - letterPosX[candWord[pos] - 'a'];
                                //    double kbdVecY = letterPosY[candWord[posL] - 'a'] - letterPosY[candWord[pos] - 'a'];

                                //    double myX = pntListX[posL] - x;
                                //    muX = xParamA * kbdVecX + xParamB * kbdVecY + xParamC;
                                //    sigmaX = xParamD * kbdVecX + xParamE * kbdVecY + xParamF;
                                //    //prob += VectorParameter.logGaussianDistribution(myX, muX, sigmaX);

                                //    double myY = pntListX[posL] - y;
                                //    muY = meanVecY[(int)kbdVecY / 40 + 2];
                                //    sigmaY = stdVecY[(int)kbdVecY / 40 + 2];
                                //    //prob += VectorParameter.logGaussianDistribution(myY, muY, sigmaY);
                                //}
                            }

                            probWords.Add(new KeyValuePair<string, double>(candWord, prob));

                            // Distance Model
                            /*
                            candWordPntL.Clear();
                            candWordPntR.Clear();
                            candWordVecL.Clear();
                            candWordVecR.Clear();

                            char lastLeft = ' ', lastRight = ' ';
                            char firstLeft = ' ', firstRight = ' ';

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
                                        else
                                        {
                                            firstLeft = currChar;
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
                                        else
                                        {
                                            firstRight = currChar;
                                        }
                                        lastRight = currChar;
                                        candWordPntR.Add(new Point(letterPosX[currCharNo], letterPosY[currCharNo]));
                                    }
                                }
                            }

                            // Calculate distance
                            //double distance = 0.0;
                            double distance = (double)(-freqDict[candWord]) / freqMax;
                            // Calculate cost(probability)
                            //double prob = Math.Log(freqDict[candWord]);

                            for (var i = 0; i < myVecL.Count; ++i)
                            {
                                //VectorParameter mVec = myVecL[i];
                                VectorParameter cwVec = candWordVecL[i];
                                //prob += cwVec.calcProbability(myVecL[i].x, myVecL[i].y);
                                distance += cwVec.getDistance(myVecL[i].x, myVecL[i].y);
                            }

                            if (myPntL.Count == 1)
                            {
                                PointParameter pp = pntParams[firstLeft];
                                distance += ( Math.Abs(myPntL[0].x - pp.userPosX) / VectorParameter.kbdSizeX +
                                    Math.Abs(myPntL[0].y  - pp.userPosY) / VectorParameter.kbdSizeY ) *
                                    Math.Exp(-candWord.Length / 10.0);

                                if (myPntR.Count > 0)
                                {
                                    distance += vecParams[firstLeft.ToString() + firstRight.ToString()].getDistance(
                                        myPntR[0].x - myPntL[0].x, myPntR[0].y - myPntL[0].y) *
                                        Math.Exp(-candWord.Length / 10.0);
                                }
                                //prob += VectorParameter.logGaussianDistribution(myPntL[0].x, pp.userPosX, pp.userStdX);
                                //prob += VectorParameter.logGaussianDistribution(myPntL[0].y, pp.userPosY, pp.userStdY);
                                
                                //VectorParameter pseudoMVec = new VectorParameter(myPntL[0].x, myPntL[0].y, 0, 0, 0);
                                //VectorParameter pseudoCWVec = new VectorParameter(candWordPntL[0].x, candWordPntL[0].y, 0, 0, 0);
                                //distance += pseudoMVec.getDistance(pseudoCWVec) * Math.Exp(-candWord.Length / 10.0);

                                //if (myPntR.Count > 0)
                                //{
                                //    VectorParameter mVec = new VectorParameter(myPntR[0].x - myPntL[0].x, myPntR[0].y - myPntL[0].y);
                                //    VectorParameter cwVec = new VectorParameter(candWordPntR[0].x - candWordPntL[0].x,
                                //        candWordPntR[0].y - candWordPntL[0].y);
                                //    distance += mVec.getDistance(cwVec) * Math.Exp(-candWord.Length / 10.0);
                                //}
                            }

                            for (var i = 0; i < myVecR.Count; ++i)
                            {
                                //VectorParameter mVec = myVecR[i];
                                VectorParameter cwVec = candWordVecR[i];
                                //prob += cwVec.calcProbability(myVecR[i].x, myVecR[i].y);
                                distance += cwVec.getDistance(myVecR[i].x, myVecR[i].y);
                            }

                            if (myPntR.Count == 1)
                            {
                                PointParameter pp = pntParams[firstRight];

                                distance += (Math.Abs(myPntR[0].x - pp.userPosX) / VectorParameter.kbdSizeX + 
                                    Math.Abs(myPntR[0].y - pp.userPosY) / VectorParameter.kbdSizeY) * 
                                    Math.Exp(-candWord.Length / 10.0);

                                if (myPntL.Count > 0)
                                {
                                    distance += vecParams[firstRight.ToString() + firstLeft.ToString()].getDistance(
                                        myPntL[0].x - myPntR[0].x, myPntL[0].y - myPntR[0].y) *
                                        Math.Exp(-candWord.Length / 10.0);
                                }
                                //prob += VectorParameter.logGaussianDistribution(myPntR[0].x, pp.userPosX, pp.userStdX);
                                //prob += VectorParameter.logGaussianDistribution(myPntR[0].y, pp.userPosY, pp.userStdY);

                                //VectorParameter pseudoMVec = new VectorParameter(myPntR[0].x, myPntR[0].y, 0, 0, 0);
                                //VectorParameter pseudoCWVec = new VectorParameter(candWordPntR[0].x, candWordPntR[0].y, 0, 0, 0);
                                //distance += pseudoMVec.getDistance(pseudoCWVec) * Math.Exp(-candWord.Length / 10.0);

                                //if (myPntL.Count > 0)
                                //{
                                //    VectorParameter mVec = new VectorParameter(myPntL[0].x - myPntR[0].x, myPntL[0].y - myPntR[0].y);
                                //    VectorParameter cwVec = new VectorParameter(candWordPntL[0].x - candWordPntR[0].x,
                                //        candWordPntL[0].y - candWordPntR[0].y);
                                //    distance += mVec.getDistance(cwVec) * Math.Exp(-candWord.Length / 10.0);
                                //}
                            }

                            // distance *= freqDict[candWord]; // / Math.Max(1, 2 - distance / freqAvg);
                            
                            probWords.Add(new KeyValuePair<string, double>(candWord, distance));
                            //Console.WriteLine("Candidates: " + candWord + ", " + distance);
                            */
                        }
                    }
                }

                probWords.Sort(MyCompareDownn);

                // Smaller distance is better. Sort Up.
                //probWords.Sort(MyCompareUp);
            }
            else if (predMode == PredictionMode.AbsoluteMode)
            {
                probWords.Clear();
                // argmax P(letter seq) * P(point pos | letter seq)
                // log version: log(P-Word) + log(P1 * P2 * P3 ... Pn) = log(P-Word) + sum(log(Pi))
                if (lenSet.ContainsKey(pntListX.Length))
                {
                    List<string> selWords = lenSet[pntListX.Length];
                    foreach (string candWord in selWords)
                    {
                        double prob = Math.Log(freqDict[candWord]);
                        for (var i = 0; i < pntListX.Length; ++i)
                        {
                            PointParameter pp = pntParams[candWord[i]];

                            double x = pntListX[i];
                            double y = pntListY[i];
                            double muX = pp.userPosX;
                            double muY = pp.userPosY;
                            double sigmaX = pp.userStdX;
                            double sigmaY = pp.userStdY;
                            double rho = pp.userCorrXY;

                            prob += VectorParameter.logBiGaussianDistribution(x, y, muX, muY, sigmaX, sigmaY, rho);

                            //double sigmaXY = sigmaX * sigmaY;
                            //double sigmaX2 = sigmaX * sigmaX;
                            //double sigmaY2 = sigmaY * sigmaY;
                            //double r = 1 - rho * rho;

                            //prob += Math.Log( 1 / (2 * Math.PI * sigmaXY * Math.Sqrt(r)) ) - 1 / (2 * r) * (
                            //    (x - muX) * (x - muX) / sigmaX2
                            //    - 2 * rho * (x - muX) * (y - muY) / sigmaXY
                            //    + (y - muY) * (y - muY) / sigmaY2 );

                            //Console.WriteLine("Old: " +　(Math.Log( 1 / (2 * Math.PI * sigmaXY * Math.Sqrt(r)) ) - 1 / (2 * r) * (
                            //    (x - muX) * (x - muX) / sigmaX2
                            //    - 2 * rho * (x - muX) * (y - muY) / sigmaXY
                            //    + (y - muY) * (y - muY) / sigmaY2 )) + "New: " + VectorParameter.logBiGaussianDistribution(x, y, muX, muY, sigmaX, sigmaY, rho));
                        }
                        probWords.Add(new KeyValuePair<string, double>(candWord, prob));
                    }

                    probWords.Sort(MyCompareDownn);
                }

                /* DFS version */
                /*
                
                currPntX = pntListX;
                currPntY = pntListY;
                int sentenceLen = currPntX.Length;
                if (sentenceLen < 10)
                    searchNum = 10;
                else if (sentenceLen < 15)
                    searchNum = 5;
                else
                    searchNum = 2;

                if (currPntX.Length == lastSentenceLen + 1)
                {
                    // Add one point
                    List<KeyValuePair<string, double>> newProbWords = new List<KeyValuePair<string, double>>();
                    for (var i = 0; i < searchNum; ++i)
                    {
                        string seq = probWords[i].Key;
                        double prob = probWords[i].Value;
                        newProbWords.AddRange(dfs(prob, currPntX.Length - 1, seq));
                    }

                    probWords = newProbWords;
                }
                else
                {
                    // Start a new one
                    probWords = dfs(10000.0, 0, "");
                }

                lastSentenceLen = currPntX.Length;
                

                // Bigger probability is better. Sort Down.
                probWords.Sort(MyCompareDownn);*/
            }
            else if (predMode == PredictionMode.DirectMode)
            {
                probWords.Clear();
                string answer = "";

                double halfKeyX = keySizeX / 2;
                double halfKeyY = keySizeY / 2;

                for (var i = 0; i < pntListX.Length; ++i)
                {
                    int hitIndex = -1;
                    double _x = pntListX[i];
                    double _y = pntListY[i];
                    for (var j = 0; j < 26; ++j)
                    {
                        if (_x >= letterPosX[j] - halfKeyX && _x <= letterPosX[j] + halfKeyX && 
                            _y >= letterPosY[j] - halfKeyY && _y <= letterPosY[j] + halfKeyY)
                        {
                            hitIndex = j;
                            break;
                        }
                    }

                    if (hitIndex >= 0)
                    {
                        // Hit a letter
                        answer += ((char)('a' + hitIndex)).ToString();
                    }
                    else if (isSpacebar(_x, _y) || isBackspace(_x, _y) || isDel(_x, _y) || isEnter(_x, _y))
                    {
                        // Hit a button
                        Console.WriteLine("++++Special Btn in predictor" + isSpacebar(_x, _y) + isBackspace(_x, _y) + isDel(_x, _y) + isEnter(_x, _y) + "(x,y):" + _x + "," + _y);
                    }
                    else
                    {
                        answer += "?";
                    }
                }
                probWords.Add(new KeyValuePair<string, double>(answer, 0));
            }
            else
            {
                Console.WriteLine("Error: Wrong Mode");
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
        //private List<KeyValuePair<string, double>> dfs(double prob, int index, string seq)
        //{
        //    List<KeyValuePair<string, double>> ret = new List<KeyValuePair<string, double>>();

        //    // Get candidates
        //    List<KeyValuePair<char, double>> tmpProbPair = new List<KeyValuePair<char, double>>();

        //    for (var i = 0; i < 26; ++i)
        //    {
        //        char c = (char)('a' + i);
        //        PointParameter pp = pntParams[c];
        //        string tmpSeq = seq + c.ToString();

        //        // Goal: argmax P(letter seq) * P(point pos | letter seq)
        //        // P(letter seq) = P(L1) * P(L2 | L1) * ... * P(Ln | Ln-k..Ln-1)
        //        // Calculate P(point pos | letter seq) = P1 * P2 * ... * Pn
        //        double currProb = biVarGaussianDistribution(currPntX[index], currPntY[index],
        //            pp.userPosX, pp.userPosY, pp.userStdX, pp.userStdY, pp.userCorrXY);
        //        int startIndex = Math.Max(0, index - wordGramLen + 1);
        //        if (index - startIndex >= 1)
        //        {
        //            currProb *= (double)(trieFreq.findChild(tmpSeq.Substring(startIndex, index - startIndex + 1))) /
        //                trieFreq.findChild(tmpSeq.Substring(startIndex, index - startIndex));
        //        }

        //        tmpProbPair.Add(new KeyValuePair<char, double>(c, currProb));
        //    }

        //    tmpProbPair.Sort(MyCompareProb);

        //    if (index == currPntX.Length - 1)
        //    {
        //        // End.
        //        for (var i = 0; i < searchNum; ++i)
        //        {
        //            ret.Add(new KeyValuePair<string, double>(seq + tmpProbPair[i].Key.ToString(), prob * tmpProbPair[i].Value));
        //        }
        //    }
        //    else
        //    {
        //        // End.
        //        for (var i = 0; i < searchNum; ++i)
        //        {
        //            ret.AddRange(dfs(prob * tmpProbPair[i].Value, index + 1, seq + tmpProbPair[i].Key.ToString()));
        //        }
        //    }

        //    return ret;
        //}

        private double biVarGaussianDistribution(double x, double y, double muX, double muY,
            double sigmaX, double sigmaY, double rho)
        {
            double sigmaXY = sigmaX * sigmaY;
            double sigmaX2 = sigmaX * sigmaX;
            double sigmaY2 = sigmaY * sigmaY;
            double r = 1 - rho * rho;

            return 1 / (2 * Math.PI * sigmaXY * Math.Sqrt(r)) * Math.Exp(-1 / (2 * r) * (
                (x-muX) * (x-muX) / sigmaX2
                - 2 * rho * (x - muX) * (y - muY) / sigmaXY
                + (y-muY) * (y-muY) / sigmaY2 ) );

            //return 1 / (2 * Math.PI * sigmaX * sigmaY * Math.Sqrt(1 - rho*rho)) 
            //    * Math.Exp(- 1 / (2 * (1 - rho*rho)) * (Math.Pow(x - muX, 2) / Math.Pow(sigmaX, 2) 
            //    - 2 * rho * (x - muX) * (y - muY) / (sigmaX * sigmaY) 
            //    + Math.Pow(y - muY, 2) / Math.Pow(sigmaY, 2)));
        }

    }
}
