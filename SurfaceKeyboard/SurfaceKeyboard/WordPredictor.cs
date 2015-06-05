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
        public bool                             loadStatus;
        public Dictionary<string, int>          freqDict;
        public Dictionary<string, List<string>> codeSet;

        // Pre-processing: Encode the words. 0:left, 1:right, 2:spacebar.
        // spacebar a b c d e f g
        // h i j k l m n
        // o p q r s t u v w x y z
        string[] handCode = {"2", "0", "0", "0", "0", "0", "0", "0", 
        "1", "1", "1", "1", "1", "1", "1",
        "1", "1", "0", "0", "0", "0", "1", "0", "0", "0", "1", "0"};

        // y = ax + b. To split two hands
        const double paramA = 2.903950;
        const double paramB = -2051.836766;

        class VectorParameter
        {
            public double vecX, vecY;
            public double vecLen;
            // rad1: [-pi,pi]   rad2: [0,2pi]
            public double rad1, rad2;

            public VectorParameter(double vX, double vY, double vLen, double r1, double r2)
            {
                vecX = vX;
                vecY = vY;
                vecLen = vLen;
                rad1 = r1;
                rad2 = r2;
            }
        }

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

        public void loadCorpus()
        {
            string jsonWordFreqName = "Resources/wordFreq.json";
            string jsonCodeSetName = "Resources/codeSet.json";
            if (!File.Exists(jsonWordFreqName) || !File.Exists(jsonCodeSetName))
            {
                // Load from txt file
                string fName = "Resources/en_US_wordlist.combined";
                string[] words = File.ReadAllLines(fName);

                for (var i = 1; i < words.Length; ++i)
                {
                    string[] wordParams = words[i].Split(',');
                    if (wordParams.Length == 4)
                    {
                        string word = wordParams[0].Split('=')[1];
                        int freq = Convert.ToInt32(wordParams[3].Split('=')[1]);

                        freqDict.Add(word, freq);

                        string code = encodeWord(word);
                        if (!codeSet.ContainsKey(code))
                        {
                            codeSet[code] = new List<string>();
                        }
                        codeSet[code].Add(word);
                    }
                }

                string jsonWord = JsonConvert.SerializeObject(freqDict, Formatting.Indented);
                //Console.WriteLine(jsonWord);
                File.WriteAllText(jsonWordFreqName, jsonWord);

                string jsonCode = JsonConvert.SerializeObject(codeSet, Formatting.Indented);
                //Console.WriteLine(jsonCode);
                File.WriteAllText(jsonCodeSetName, jsonCode);

                Console.WriteLine("JSON Object Saved.");
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

                Console.WriteLine("JSON Object Load");
            }
            loadStatus = true;
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

            foreach (string userCode in userCodes)
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
                            double vecLen = Math.Sqrt(Math.Pow(vecX, 2) + Math.Pow(vecY, 2));
                            double rad1 = 0.0, rad2 = 0.0;
                            if (vecLen > 0)
                            {
                                if (vecY > 0)
                                {
                                    rad1 = Math.Acos(vecX / vecLen);
                                    rad2 = rad1;
                                }
                                else
                                {
                                    rad1 = -Math.Acos(vecX / vecLen);
                                    rad2 = Math.PI + Math.Acos(-vecX / vecLen);
                                }
                            }

                            myVecL.Add(new VectorParameter(vecX, vecY, vecLen, rad1, rad2));
                        }
                        myPntL.Add(new Point(pntListX[i], pntListY[i]));
                    }
                    else
                    {
                        if (myPntR.Count > 0)
                        {
                            double vecX = pntListX[i] - myPntR.Last().x;
                            double vecY = pntListY[i] - myPntR.Last().y;
                            double vecLen = Math.Sqrt(Math.Pow(vecX, 2) + Math.Pow(vecY, 2));
                            double rad1 = 0.0, rad2 = 0.0;
                            if (vecLen > 0)
                            {
                                if (vecY > 0)
                                {
                                    rad1 = Math.Acos(vecX / vecLen);
                                    rad2 = rad1;
                                }
                                else
                                {
                                    rad1 = -Math.Acos(vecX / vecLen);
                                    rad2 = Math.PI + Math.Acos(-vecX / vecLen);
                                }
                            }

                            myVecR.Add(new VectorParameter(vecX, vecY, vecLen, rad1, rad2));
                        }
                        myPntR.Add(new Point(pntListX[i], pntListY[i]));
                    }
                }
            }
            //probWords.Add("Hello World!");
            return probWords;
        }
    }
}
