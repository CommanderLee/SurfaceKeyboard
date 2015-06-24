using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestSimulator
{
    enum ErrorType { Other, Correct, WholeWord };

    class WordClassifier
    {
        public string targetWord;
        public string userID;

        public List<double> xList;
        public List<double> yList;

        public string pointTypes;
        public ErrorType errorType;

        public WordClassifier()
        {
            xList = new List<double>();
            yList = new List<double>();
            pointTypes = "";
            errorType = ErrorType.Other;
        }
    }
}
