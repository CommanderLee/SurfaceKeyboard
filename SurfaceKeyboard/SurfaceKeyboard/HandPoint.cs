using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    class HandPoint
    {
        private double _x;
        private double _y;
        private double _time;
        private string _id;

        public HandPoint(double x, double y, double time, string id)
        {
            _x = x;
            _y = y;
            _time = time;
            _id = id;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}, {3}", _x, _y, _time, _id);
        }

        public double getX() { return _x; }
        public double getY() { return _y; }
        public double getTime() { return _time; }
    }
}
