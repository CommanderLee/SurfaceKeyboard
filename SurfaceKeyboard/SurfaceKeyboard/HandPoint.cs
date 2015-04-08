using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    enum HPType { Touch, Move, Calibrate };

    /* Basic Point Class */
    class BasicPoint
    {
        public BasicPoint() { }
        public BasicPoint(double _x, double _y)
        {
            x = _x;
            y = _y;
        }

        public double x;
        public double y;
    }

    /**
     * Elemental class for this project.
     * Represent each single data point.
     */
    class HandPoint
    {
        /**
         * pt: point
         * time: time stamp
         * id: test set number and sentence number
         * type: touch or move
         */
        private BasicPoint _pt;
        private double _time;
        private string _id;
        private HPType _type;

        public HandPoint()
        {
            _pt = new BasicPoint();
        }

        public HandPoint(double x, double y, double time, string id, HPType type)
        {
            _pt = new BasicPoint();

            _pt.x = x;
            _pt.y = y;
            _time = time;
            _id = id;
            _type = type;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}, {3}, {4}", _pt.x, _pt.y, _time, _id, _type);
        }

        public double getX() { return _pt.x; }
        public double getY() { return _pt.y; }
        public double getTime() { return _time; }
        public String getId() { return _id; }
        public void setId(String id) { _id = id; }
    }
}
