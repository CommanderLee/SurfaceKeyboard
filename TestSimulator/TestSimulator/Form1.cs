using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics.Contracts;

namespace TestSimulator
{
    public partial class Form1 : Form
    {
        WordClassifier currWord = null;
        WordPredictor currPredictor;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            currPredictor = new WordPredictor();

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Multiselect = true;
            openFile.Filter = "Data Files (*.csv)|*.csv|" + "All Files (*.*)|*.*";
            string initDir = "E:\\Code\\Research-2014-Fall\\Surface\\SurfaceKeyboard\\SurfaceKeyboard\\SurfaceKeyboard\\bin\\Debug";
            if (Directory.Exists(initDir))
            {
                openFile.InitialDirectory = initDir;
            }

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                work(openFile.FileNames);
            }
        }

        private void work(string[] fileNames)
        {
            Console.WriteLine("Load " + fileNames.Length + " files.");
            string totalLines = "";
            string csvHeader = "TargetWord, UserID, XList, YList, PointTypes, ErrorType\n";

            foreach (string fileName in fileNames)
            {
                currWord = null;
                string saveLines = "";
                string resultFile = fileName.Substring(0, fileName.Length - 9) + "Result.csv";

                Console.WriteLine(fileName);
                string[] dataLines = File.ReadAllLines(fileName);

                // Load TaskText File with the same prefix(same test)
                string textFile = fileName.Substring(0, fileName.Length - 9) + "TaskText.txt";
                string[] textLines = File.ReadAllLines(textFile);

                // Pick up user id / predict mode from file name
                string[] fileParams = fileName.Split('\\').Last().Split('_');
                Contract.Assert(fileParams.Length >= 7);

                string userID = fileParams[4];
                string predictMode = fileParams[6];
                Console.WriteLine("User: " + userID + " Mode: " + predictMode);

                int textIndex = 0;
                int wordIndex = 0;
                string[] words = textLines[textIndex].Trim().Split(' ');

                for (var i = 1; i < dataLines.Length; ++i)
                {
                    string dataStr = dataLines[i];
                    if (dataStr.Length > 1)
                    {
                        // Parse current point information
                        string[] dataArray = dataStr.Split(',');

                        Contract.Assert(dataArray.Length == 5);
                        double x = Convert.ToDouble(dataArray[0]);
                        double y = Convert.ToDouble(dataArray[1]);
                        double time = Convert.ToDouble(dataArray[2]);
                        string[] pointIDArray = dataArray[3].Split('_');
                        string pointType = dataArray[4].Trim()[0].ToString();

                        Contract.Assert(pointIDArray.Length == 3);
                        int taskIndex = Convert.ToInt32(pointIDArray[0]);

                        if (currPredictor.isSpacebar(x, y))
                        {
                            pointType += "S";
                        }

                        // Update current word classifier
                        if (currWord == null)
                        {
                            // A new word:
                            currWord = new WordClassifier();
                            //currWord.targetWord = textLines[textIndex];
                            if (wordIndex >= words.Length)
                            {
                                currWord.targetWord = "?";
                            }
                            else
                            {
                                currWord.targetWord = words[wordIndex];
                            }
                            currWord.userID = userID + predictMode.Trim()[0].ToString();
                            currWord.pointTypes = pointType;
                            currWord.errorType = ErrorType.Other;
                        }
                        else
                        {
                            // Update:
                            currWord.pointTypes += "-" + pointType;
                        }
                        // Common for New/Update
                        currWord.xList.Add(x);
                        currWord.yList.Add(y);

                        // If finish a word, save.
                        // Peek next
                        bool isEnd = false;
                        if (i == dataLines.Length - 1 || pointType == "TS")
                        {
                            isEnd = true;
                        }
                        else
                        {
                            string dataStrNext = dataLines[i + 1];
                            if (dataStrNext.Length > 1)
                            {
                                // Parse current point information
                                string[] dataArrayNext = dataStrNext.Split(',');

                                Contract.Assert(dataArrayNext.Length == 5);
                                //double x = Convert.ToDouble(dataArray[0]);
                                //double y = Convert.ToDouble(dataArray[1]);
                                //double time = Convert.ToDouble(dataArray[2]);
                                string[] pointIDArrayNext = dataArrayNext[3].Split('_');
                                //string pointType = dataArray[4][0].ToString();

                                Contract.Assert(pointIDArrayNext.Length == 3);
                                int taskIndexNext = Convert.ToInt32(pointIDArrayNext[0]);

                                // Now test with sentence-based cut.
                                // TODO: Replace with word-based cut.
                                if (taskIndex != taskIndexNext)
                                {
                                    isEnd = true;
                                }
                            }
                        }

                        // End of a word (or sentence)
                        if (isEnd)
                        {
                            // Judge error type
                            if (wordIndex < words.Length)
                            {
                                currWord.updateErrorType(words[wordIndex].Length);
                            }
                            // Save
                            saveLines += currWord.ToString() + "\n";

                            // Clear
                            wordIndex++;
                            if (wordIndex == words.Length && textIndex + 1 < textLines.Length)
                            {
                                textIndex++;
                                wordIndex = 0;
                                words = textLines[textIndex].Trim().Split(' ');
                            }
                            //textIndex++;
                            currWord = null;
                        }
                    }
                }

                File.WriteAllText(resultFile, csvHeader + saveLines);
                Console.WriteLine(resultFile.Split('\\').Last() + " Done.");
                totalLines += saveLines;
            }

            string totalFile = String.Format("{0}users_total.csv", fileNames.Length);
            File.WriteAllText(totalFile, csvHeader + totalLines);
            Console.WriteLine(totalFile + " Done.");
        }

    }
}
