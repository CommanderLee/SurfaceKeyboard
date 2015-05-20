using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    enum HandStatus { Away, Backspace, Enter, Type, Rest, Unknown };

    /// <summary>
    /// A queue of consecutive data points.
    /// Some methods to recognize different gestures.
    /// </summary>
    class GesturePoints
    {
        /**
        * Constants for the gesture
        * - A gesture will be triggered if it occured within the move time limit
        * - Touch time limit: touch time min < touch time <  touch time max
        * - xxxTHRE: Length threshold (pixels) for backspace and enter
        */
        private const double MOVE_TIME_LIMIT = 300;
        private const double TOUCH_TIME_MAX = 10000;
        private const double TOUCH_TIME_MIN = 0;
        private const double BACK_THRE = 150;
        private const double ENTER_THRE = 150;

        private Queue<HandPoint> _queue;
        private HandStatus       _status;
        private double           _startTime = -1;
        private HandPoint        _startPoint;

        private static bool      gestureSwitch = true;
        public static bool getGestureSwitch() { return gestureSwitch; }
        public static void reverseGestureSwitch() { gestureSwitch = !gestureSwitch; }

        public GesturePoints()
        {
            _queue = new Queue<HandPoint>();
        }

        public GesturePoints(HandPoint startPoint, HandStatus status)
        {
            _queue = new Queue<HandPoint>();
            _queue.Enqueue(startPoint);

            _startTime = startPoint.getTime();
            _startPoint = startPoint;

            _status = status;
            Console.WriteLine("Create new start point:" + _startPoint.ToString());
        }

        public void Add(HandPoint handPoint)
        {
            //if (_startTime < 0)
            //{
            //    _startTime = handPoint.getTime();
            //}

            while (_queue.Count > 0 && handPoint.getTime() - _queue.Peek().getTime() > MOVE_TIME_LIMIT)
            {
                _queue.Dequeue();
            }
            _queue.Enqueue(handPoint);
        }

        public void setStatus(HandStatus status)
        {
            _status = status;
        }

        public HandStatus getStatus()
        {
            return _status;
        }

        public HandPoint getStartPoint()
        {
            return _startPoint;
        }

        /**
         * Calculate moving distance and update gesture status
         * positive: enter, negative: backspace, 0: typing
         */
        public HandStatus updateGestureStatus()
        {
            HandPoint lastHP = _queue.First();
            double sumDist = 0.0;
            int directionCnt = 0;

            foreach (HandPoint hPoint in _queue)
            {
                if (hPoint.getX() > lastHP.getX())
                {
                    ++directionCnt;
                }
                else if (hPoint.getX() < lastHP.getX())
                {
                    --directionCnt;
                }

                sumDist += Math.Sqrt(Math.Pow(hPoint.getX() - lastHP.getX(), 2) + 
                    Math.Pow(hPoint.getY() - lastHP.getY(), 2));
                lastHP = hPoint;
            }

            if (directionCnt < 0 && sumDist > BACK_THRE)
            {
                _status = HandStatus.Backspace;
                _startPoint.setType(HPType.Delete);
            }
            else if (directionCnt > 0 && sumDist > ENTER_THRE)
            {
                _status = HandStatus.Enter;
            }

            return _status;
        }

        /**
         * Check typing based on the movement within a single touch.
         */
        public bool checkTyping()
        {
            bool isTyping = false;
            if (_queue.Count == 0)
            {
                Console.WriteLine("[Error] checkTyping(): The queue is empty.");
            }
            else
            {
                // TODO: Use distance or variance. 
                isTyping = true;
                _status = HandStatus.Type;

                //double touchTime = _queue.Last().getTime() - _startTime;
                //if (touchTime <= TOUCH_TIME_MAX && touchTime >= TOUCH_TIME_MIN)
                //{
                //    isTyping = true;
                //    _status = HandStatus.Type;
                //}
                
            }
            return isTyping;
        }

    }
}
