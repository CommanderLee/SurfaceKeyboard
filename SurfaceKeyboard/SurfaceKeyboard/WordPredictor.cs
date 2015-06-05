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
    }
}
