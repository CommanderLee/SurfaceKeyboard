using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestSimulator
{
    enum ErrorType { Other, AllCorrect, RecoverCorrect, DeleteAll, };

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

        public override string ToString()
        {
            string xListStr = String.Join("-", xList);
            string yListStr = String.Join("-", yList);

            string result = "";
            result = String.Format("{0}, {1}, {2}, {3}, {4}, {5}", targetWord, userID, xListStr, yListStr, pointTypes, errorType);

            return result;
        }

        public void updateErrorType(int len)
        {
            string[] pointTypeArray = pointTypes.Split('-');

            int tNum = 0, dNum = 0, rNum = 0;
            int dNumPrefix = 0;
            foreach (string pType in pointTypeArray)
            {
                if (pType == "T")
                    tNum++;
                else if (pType == "D" || pType == "DS")
                    dNum++;
                else if (pType == "R")
                    rNum++;

                if (tNum == 0 && rNum == 0 && pType[0] == 'D')
                    dNumPrefix++;
            }

            if (tNum == len && rNum == 0 && dNum == 0)
                errorType = ErrorType.AllCorrect;
            else if (rNum > 0 && tNum + rNum == len && dNum == 0)
                errorType = ErrorType.RecoverCorrect;
            else if (dNumPrefix == dNum && tNum + rNum == len)
                errorType = ErrorType.DeleteAll;
            else
            {

            }
            //if (pointTypeArray.Length == len)
            //{

            //}
        }
    }
}
