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
        /* Ten fingers: Refer to the standard hand position.
         * 0:left litte finger, 4:left thumb, 5:right thumb, 9:right little finger, and so on.  */
        private HandPoint[] fingerPoints;
        public const int FINGER_NUMBER = 10;
        
        public HandModel()
        {
            fingerPoints = new HandPoint[10];
        }

        /* Calculate center points and sort finger list */
        public void loadHandPoints(List<HandPoint> points)
        {

        }
    }
}
