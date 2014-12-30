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
        private string HP_ID;

        public HandPoint(double x, double y, double time, string id)
        {
            HP_X = x;
            HP_Y = y;
            HP_Time = time;
            HP_ID = id;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}, {3}", HP_X, HP_Y, HP_Time, HP_ID);
        }
    }
}
