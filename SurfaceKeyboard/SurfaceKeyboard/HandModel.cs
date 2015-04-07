using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    /**
     * Hand Model for typing calibration.
     * Use ten-finger touch points, calculate sorted list and center point.
     */ 
    class HandModel
    {
        public const int    FINGER_NUMBER = 10;

        /* Ten fingers: Refer to the standard hand position.
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

        /* Just for swapping the fingerPoints */
        private void swap(int pos1, int pos2)
        {
            if (pos1 >= 0 && pos1 < fingerPoints.Length && pos2 >= 0 && pos2 < fingerPoints.Length && pos1 != pos2)
            {
                HandPoint temp = fingerPoints[pos1];
                fingerPoints[pos1] = fingerPoints[pos2];
                fingerPoints[pos2] = temp;
            }
        }

        /* Sort the finger points refer to standard typing gesture */
        private void sortFingerPoints(List<HandPoint> points)
        {
            fingerPoints = points.ToArray();
            //Console.WriteLine("Before sorting");
            //foreach (HandPoint point in fingerPoints)
            //    Console.WriteLine(point.ToString());

            // Sort by X-Coordinate
            Array.Sort(fingerPoints, delegate(HandPoint point1, HandPoint point2)
            {
                return point1.getX().CompareTo(point2.getX());
            });

            // Find thumb index (Max Y-Coordinate)
            int leftThumb = 0, rightThumb = 5;
            double leftThumbY = fingerPoints[leftThumb].getY();
            double rightThumbY = fingerPoints[rightThumb].getY();

            for (int i = 1; i < 5; ++i)
            {
                if (fingerPoints[i].getY() > leftThumbY)
                {
                    leftThumbY = fingerPoints[i].getY();
                    leftThumb = i;
                }
            }
            for (int i = 6; i < 10; ++i)
            {
                if (fingerPoints[i].getY() > rightThumbY)
                {
                    rightThumbY = fingerPoints[i].getY();
                    rightThumb = i;
                }
            }

            // Swap the thumb points to the supposed position
            swap(leftThumb, 4);
            swap(rightThumb, 5);

            //Console.WriteLine("After sorting");
            //foreach (HandPoint point in fingerPoints)
            //    Console.WriteLine(point.ToString());
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



                return true;
            }
            else
                return false;
        }
    }
}
