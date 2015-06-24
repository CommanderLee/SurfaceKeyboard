using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace TestSimulator
{
    public partial class Form1 : Form
    {
        WordClassifier currWord = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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
                Console.WriteLine("Load " + openFile.FileNames.Length + " files.");
                foreach (string fileName in openFile.FileNames)
                {
                    Console.WriteLine(fileName);
                    string[] dataLines = File.ReadAllLines(fileName);

                    // Load TaskText File with the same prefix(same test)
                    string textFile = fileName.Substring(0, fileName.Length - 9) + "TaskText.txt";
                    string[] textLines = File.ReadAllLines(textFile);

                    // Pick up user id / predict mode from file name
                    string[] fileParams = fileName.Split('\\').Last().Split('_');
                    if (fileParams.Length >= 7)
                    {
                        string userID = fileParams[4];
                        string predictMode = fileParams[6];
                        Console.WriteLine("User: " + userID + " Mode: " + predictMode);
                    }
                    else
                    {
                        Console.WriteLine("Error: Invalid File.");
                    }

                    int textIndex = 0;
                    int wordIndex = 0;

                    for (var i = 1; i < dataLines.Length; ++i)
                    {
                        string dataStr = dataLines[i];
                        if (dataStr.Length > 1)
                        {
                            string[] dataArray = dataStr.Split(',');
                            if (currWord == null)
                            {
                                // A new word
                                currWord = new WordClassifier();
                                currWord.targetWord = textLines[textIndex].Split(' ')[wordIndex];
                            }


                        }
                    }
                }
            }
        }
    }
}
