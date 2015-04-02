using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    enum HPType { Touch, Move, Calibrate };

    /**
     * Elemental class for this project.
     * Represent each single data point.
     */
    class HandPoint
    {
        /**
         * x, y: coordinate
         * time: time stamp
         * id: test set number and sentence number
         * type: touch or move
         */
        private double _x;
        private double _y;
        private double _time;
        private string _id;
        private HPType _type;

        public HandPoint(double x, double y, double time, string id, HPType type)
        {
            _x = x;
            _y = y;
            _time = time;
            _id = id;
            _type = type;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}, {3}, {4}", _x, _y, _time, _id, _type);
        }

        public double getX() { return _x; }
        public double getY() { return _y; }
        public double getTime() { return _time; }
        public String getId() { return _id; }
    }
}
