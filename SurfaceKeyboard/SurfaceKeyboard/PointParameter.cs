using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    class PointParameter
    {
        //public char     userChar;
        public double   userPosX, userPosY;
        public double   userStdX, userStdY;
        public double   userVarX, userVarY;
        public double   userCovXY, userCorrXY;

        public PointParameter() { }

        public PointParameter(double uPX, double uPY, double uSX, double uSY,
            double uVX, double uVY, double uCXY, double uCRXY)
        {
            //userChar = uC;
            userPosX = uPX;
            userPosY = uPY;
            userStdX = uSX;
            userStdY = uSY;
            userVarX = uVX;
            userVarY = uVY;
            userCovXY = uCXY;
            userCorrXY = uCRXY;
        }
    }

}
