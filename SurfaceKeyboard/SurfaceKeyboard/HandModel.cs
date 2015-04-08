using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    /* Left/Right + Little/Ring/Middle/Index/Thumb  */
    enum FingerCode { LeftLittle = 0, LeftRing, LeftMiddle, LeftIndex, LeftThumb,
    RightThumb, RightIndex, RightMiddle, RightRing, RightLittle };

    /**
     * Hand Model for typing calibration.
     * Use ten-finger touch points, calculate sorted list and center point.
     */ 
    class HandModel
    {
        public const int    FINGER_NUMBER = 10;

        /* Ten fingers: Refer to the standard hand position. Enum: Finger Code.
         * 0:left litte finger, 4:left thumb, 5:right thumb, 9:right little finger, and so on.  */
        private HandPoint[] fingerPoints;

        /* Center points of two hands, left hand, and right hand */
        private BasicPoint  centerPt, leftCenterPt, rightCenterPt;

        public HandModel()
        {
            fingerPoints = new HandPoint[10];

            centerPt = new BasicPoint();
            leftCenterPt = new BasicPoint();
            rightCenterPt = new BasicPoint();
        }

        /* Sort the finger points refer to standard typing gesture */
        private void sortFingerPoints(List<HandPoint> points)
        {
            fingerPoints = points.ToArray();
            //Console.WriteLine("Before sorting");
            //foreach (HandPoint point in fingerPoints)
            //    Console.WriteLine(point.ToString());

            /* Sort by X-Coordinate */
            Array.Sort(fingerPoints, delegate(HandPoint point1, HandPoint point2)
            {
                return point1.getX().CompareTo(point2.getX());
            });

            /* Find thumb index (Max Y-Coordinate) */
            int leftThumb = (int)FingerCode.LeftThumb, rightThumb = (int)FingerCode.RightThumb;
            double leftThumbY = fingerPoints[leftThumb].getY();
            double rightThumbY = fingerPoints[rightThumb].getY();

            for (int i = (int)FingerCode.LeftLittle; i <= (int)FingerCode.LeftThumb; ++i)
            {
                if (fingerPoints[i].getY() > leftThumbY)
                {
                    leftThumbY = fingerPoints[i].getY();
                    leftThumb = i;
                }
            }
            for (int i = (int)FingerCode.RightThumb; i <= (int)FingerCode.RightLittle; ++i)
            {
                if (fingerPoints[i].getY() > rightThumbY)
                {
                    rightThumbY = fingerPoints[i].getY();
                    rightThumb = i;
                }
            }

            Console.WriteLine("After sorting");
            foreach (HandPoint point in fingerPoints)
                Console.WriteLine(point.ToString());

            /* Move the thumb to its place */
            HandPoint leftThumbPoint = fingerPoints[leftThumb], rightThumbPoint = fingerPoints[rightThumb];
            
            for (int i = leftThumb + 1; i <= (int)FingerCode.LeftThumb; ++i)
            {
                fingerPoints[i - 1] = fingerPoints[i];
            }
            fingerPoints[(int)FingerCode.LeftThumb] = leftThumbPoint;

            for (int i = rightThumb - 1; i >= (int)FingerCode.RightThumb; --i)
            {
                fingerPoints[i + 1] = fingerPoints[i];
            }
            fingerPoints[(int)FingerCode.RightThumb] = rightThumbPoint;

            Console.WriteLine("After Swapping");
            foreach (HandPoint point in fingerPoints)
                Console.WriteLine(point.ToString());

            /* Change the finger id */
            for (int i = (int)FingerCode.LeftLittle; i <= (int)FingerCode.RightLittle; ++i)
            {
                String currId = fingerPoints[i].getId();
                currId = currId.Substring(0, currId.Length - 2) + i;
                fingerPoints[i].setId(currId);
            }

            Console.WriteLine("After changing the finger id");
            foreach (HandPoint point in fingerPoints)
                Console.WriteLine(point.ToString());
        }

        /* Calculate center points of user's hands */
        private void calcCenterPoints()
        {
            // About the magic number 0, 4, 6, 10: Refer to the hand position
            double leftXSum = 0, leftYSum = 0, rightXSum = 0, rightYSum = 0;
            for (int i = 0; i < 4; ++i)
            {
                leftXSum += fingerPoints[i].getX();
                leftYSum += fingerPoints[i].getY();
            }
            for (int i = 6; i < 10; ++i)
            {
                rightXSum += fingerPoints[i].getX();
                rightYSum += fingerPoints[i].getY();
            }
            
            // TODO: Get the position.
        }

        /* Calculate center points and sort finger list */
        public bool loadHandPoints(List<HandPoint> points)
        {
            if (points.Count == FINGER_NUMBER)
            {
                sortFingerPoints(points);

                calcCenterPoints();

                return true;
            }
            else
                return false;
        }

        /* Get finger points */
        public HandPoint[] getFingerPoints()
        {
            return fingerPoints;
        }
    }
}
