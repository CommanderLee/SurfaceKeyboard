using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    enum HandStatus { Away, Backspace, Enter, Type, Rest, Wait, Unknown };

    /// <summary>
    /// A queue of consecutive data points.
    /// Some methods to recognize different gestures.
    /// </summary>
    class GesturePoints
    {
        // G_T_Max: max time sequence for the queue. Only nearest points are saved.
        // G_T_Min: min time threshold for the gesture checking
        const double     GESTURE_TIME_MAX = 400;
        //private const double TOUCH_TIME_MAX = 10000;
        const double     GESTURE_TIME_MIN = 300;

        // Distance threshold for backspace and enter
        const double     BACK_DIST_THRE = 200;
        const double     ENTER_DIST_THRE = 200;

        Queue<HandPoint> _queue;
        HandStatus       _status;
        double           _startTime = -1;
        HandPoint        _startPoint;

        int              movingDirection;
        double           movingDistance;

        private static bool     gestureSwitch = true;
        public static bool      getGestureSwitch() { return gestureSwitch; }
        public static void      reverseGestureSwitch() { gestureSwitch = !gestureSwitch; }

        public GesturePoints()
        {
            _queue = new Queue<HandPoint>();
            _status = HandStatus.Wait;
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

            while (_queue.Count > 0 && handPoint.getTime() - _queue.Peek().getTime() > GESTURE_TIME_MAX)
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

        public double calcMovingParam()
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

            movingDirection = directionCnt;
            movingDistance = sumDist;

            return sumDist;
        }

        /**
         * Calculate moving distance and update gesture status
         * positive: enter, negative: backspace, 0: typing
         */
        public HandStatus updateGestureStatus()
        {
            double touchTime = _queue.Last().getTime() - _startTime;
            if (touchTime > GESTURE_TIME_MIN)
            {
                calcMovingParam();

                if (movingDirection < 0 && movingDistance > BACK_DIST_THRE)
                {
                    _status = HandStatus.Backspace;
                    _startPoint.setType(HPType.Delete);
                }
                else if (movingDirection > 0 && movingDistance > ENTER_DIST_THRE)
                {
                    _status = HandStatus.Enter;
                }
                else
                {
                    _status = HandStatus.Unknown;
                }
            }
            else
            {
                Console.WriteLine("Current touch time:" + touchTime);
                _status = HandStatus.Wait;
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
