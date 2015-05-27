using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace SurfaceKeyboard
{
    enum HPType { Touch, Move, Release, Calibrate, Delete, Recover };

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
        private Point _pt;
        //private BasicPoint _pt;
        private double _time;
        private string _id;
        private HPType _type;

        public HandPoint()
        {
            _pt = new Point();
        }

        public HandPoint(double x, double y, double time, string id, HPType type)
        {
            _pt = new Point(x, y);

            //_pt.x = x;
            //_pt.y = y;
            _time = time;
            _id = id;
            _type = type;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}, {3}, {4}", _pt.X, _pt.Y, _time, _id, _type);
        }

        public double getX() { return _pt.X; }
        public double getY() { return _pt.Y; }

        public double getTime() { return _time; }

        public String getId() { return _id; }
        public void setId(String id) { _id = id; }

        public void setType(HPType type) { _type = type; }
    }
}
