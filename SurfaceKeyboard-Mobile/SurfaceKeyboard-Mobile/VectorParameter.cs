using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard_Mobile
{
    class VectorParameter
    {
        public double vecX, vecY;
        public double vecLen;
        // rad1: [-pi,pi]   rad2: [0,2pi]
        public double rad1, rad2;

        public double stdVecX, stdVecY;
        public double stdVecLen;
        public double radDist;

        // Key-Size
        //static double keySizeX = 39.0;
        //static double keySizeY = 42.0;
        public static double keySizeLen = 57.3149195236284;

        // Keyboard-Size
        public static double kbdSizeX = 407.0;
        public static double kbdSizeY = 175.0;
        public static double kbdSizeLen = 443.02821580572044;

        static double log2pi = Math.Log(2 * Math.PI);

        public VectorParameter() { }

        public VectorParameter(double vX, double vY, double vLen, double r1, double r2,
            double sVX, double sVY, double sVL, double rD)
        {
            vecX = vX;
            vecY = vY;
            vecLen = vLen;
            rad1 = r1;
            rad2 = r2;

            stdVecX = sVX;
            stdVecY = sVY;
            stdVecLen = sVL;
            radDist = rD;

            if (vecLen < 0)
            {
                // Calculate
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
        }

        //public VectorParameter(double vX, double vY)
        //{
        //    vecX = vX;
        //    vecY = vY;

        //    vecLen = Math.Sqrt(Math.Pow(vecX, 2) + Math.Pow(vecY, 2));
        //    rad1 = 0.0;
        //    rad2 = 0.0;
        //    if (vecLen > 0)
        //    {
        //        if (vecY > 0)
        //        {
        //            rad1 = Math.Acos(vecX / vecLen);
        //            rad2 = rad1;
        //        }
        //        else
        //        {
        //            rad1 = -Math.Acos(vecX / vecLen);
        //            rad2 = Math.PI + Math.Acos(-vecX / vecLen);
        //        }
        //    }
        //}

        /// <summary>
        /// A Pseudo-Probability. Using p.d.f.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public double calcProbability(double x, double y)
        {
            double len = Math.Sqrt(x * x + y * y);
            double r1 = 0.0;
            double r2 = 0.0;
            if (len > 0)
            {
                if (y > 0)
                {
                    r1 = Math.Acos(x / len);
                    r2 = rad1;
                }
                else
                {
                    r1 = -Math.Acos(x / len);
                    r2 = Math.PI + Math.Acos(-x / len);
                }
            }

            // Log version
            double prob = 0.0;
            prob += logGaussianDistribution(x, vecX, stdVecX);
            //Console.WriteLine("    " + x + "," + vecX + "," + y + "," + vecY);
            //Console.WriteLine("    prob:" + prob);
            prob += logGaussianDistribution(y, vecY, stdVecY);
            prob += logGaussianDistribution(len, vecLen, stdVecLen);

            double rd = 0.0;
            if (vecLen >= keySizeLen)
            {
                rd = Math.Min(Math.Abs(rad1 - r1), Math.Abs(rad2 - r2));
                prob += logExponentialDistribution(rd, 1 / radDist);
            }
            //Console.WriteLine("    " + x + "," + vecX + "," + y + "," + vecY);
            //Console.WriteLine("    prob:" + prob);
            return prob;
        }


        public static double logGaussianDistribution(double x, double mu, double sigma)
        {
            double ret = -log2pi - Math.Log(sigma) - (x - mu) * (x - mu) / (2 * sigma * sigma);
            //Console.WriteLine("LogNorm:" + x + ", " + mu + ", " + sigma + "value: " + ret);

            return ret;
        }

        public static double logBiGaussianDistribution(double x, double y, double muX, double muY, double sigmaX, double sigmaY, double rho)
        {
            double sigmaXY = sigmaX * sigmaY;
            double sigmaX2 = sigmaX * sigmaX;
            double sigmaY2 = sigmaY * sigmaY;
            double r = 1 - rho * rho;

            double ret = -((x - muX) * (x - muX) / sigmaX2 - 2 * rho * (x - muX) * (y - muY) / sigmaXY + (y - muY) * (y - muY) / sigmaY2)
                / (2 * r) - log2pi - Math.Log(sigmaXY) - 0.5 * Math.Log(r);

            return ret;
        }

        public static double logExponentialDistribution(double x, double lambda)
        {
            return Math.Log(lambda) - lambda * x;
        }

        public static double gaussianDistribution(double x, double mu, double sigma)
        {
            return 1 / (Math.Sqrt(2 * Math.PI) * sigma) * Math.Exp(-(x - mu) * (x - mu) / (2 * sigma * sigma));
        }

        public static double exponentialDistribution(double x, double lambda)
        {
            return lambda * Math.Exp(-lambda * x);
        }

        public double getDistance(double x, double y)
        {
            double len = Math.Sqrt(x * x + y * y);
            double r1 = 0.0;
            double r2 = 0.0;
            if (len > 0)
            {
                if (y > 0)
                {
                    r1 = Math.Acos(x / len);
                    r2 = rad1;
                }
                else
                {
                    r1 = -Math.Acos(x / len);
                    r2 = Math.PI + Math.Acos(-x / len);
                }
            }

            double distX = Math.Abs(vecX - x);
            double distY = Math.Abs(vecY - y);
            double distLen = Math.Abs(vecLen - len);

            // Ignore this item if two character is too close
            double distRad = 0.0;
            if (vecLen >= keySizeLen)
            {
                distRad = Math.Min(Math.Abs(rad1 - r1), Math.Abs(rad2 - r2));
            }
            else
            {
                distX *= 2;
                distY *= 2;
                distLen *= 2;
            }

            double dist = distLen / kbdSizeLen + distRad / Math.PI + distX / kbdSizeX + distY / kbdSizeY;
            return dist;
        }
    }
}
