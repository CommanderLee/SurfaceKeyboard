using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    class HandPoint
    {
        private double HP_X;
        private double HP_Y;
        private double HP_Time;

        public HandPoint(double x, double y, double time)
        {
            HP_X = x;
            HP_Y = y;
            HP_Time = time;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}", HP_X, HP_Y, HP_Time);
        }
    }
}
