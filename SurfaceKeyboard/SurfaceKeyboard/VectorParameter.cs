using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    class VectorParameter
    {
        public double vecX, vecY;
        public double vecLen;
        // rad1: [-pi,pi]   rad2: [0,2pi]
        public double rad1, rad2;

        // Key-Size
        static double keySizeX = 39.0;
        static double keySizeY = 42.0;
        static double keySizeLen = 57.3149195236284;

        // Keyboard-Size
        static double kbdSizeX = 407.0;
        static double kbdSizeY = 175.0;
        static double kbdSizeLen = 443.02821580572044;

        public VectorParameter() { }

        public VectorParameter(double vX, double vY, double vLen, double r1, double r2)
        {
            vecX = vX;
            vecY = vY;
            vecLen = vLen;
            rad1 = r1;
            rad2 = r2;
        }

        public VectorParameter(double vX, double vY)
        {
            vecX = vX;
            vecY = vY;

            vecLen = Math.Sqrt(Math.Pow(vecX, 2) + Math.Pow(vecY, 2));
            rad1 = 0.0;
            rad2 = 0.0;
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
        }

        public double getDistance(VectorParameter vP)
        {
            double distX = Math.Abs(vecX - vP.vecX);
            double distY = Math.Abs(vecY - vP.vecY);
            double distLen = Math.Abs(vecLen - vP.vecLen);
            double distRad = Math.Min(Math.Abs(rad1 - vP.rad1), Math.Abs(rad2 - vP.rad2));

            double dist = distLen / kbdSizeLen + distRad / Math.PI + distX / kbdSizeX + distY / kbdSizeY;
            return dist;
        }
    }
}
