using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard
{
    enum HandStatus { Away, Backspace, Enter, Type, Rest };
    class GesturePoints
    {
        /**
        * Constants for the gesture
        * - A gesture will be triggered if it occured within the move time limit
        * - Recognized as a touch is released within the touch time limit
        * - xxxTHRE: Length threshold (pixels) for backspace and enter
        */
        private const double MOVE_TIME_LIMIT = 100;
        private const double TOUCH_TIME_LIMIT = 500;
        private const double BACK_THRE = 100;
        private const double ENTER_THRE = 100;

        private Queue<HandPoint> _queue;
        private HandStatus       _status;
        private double           _startTime = -1;

        public GesturePoints()
        {
            _queue = new Queue<HandPoint>();
        }

        public GesturePoints(HandStatus status)
        {
            _queue = new Queue<HandPoint>();
            _status = status;
        }

        public void Add(HandPoint handPoint)
        {
            if (_startTime < 0)
            {
                _startTime = handPoint.getTime();
            }

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

        /**
         * Check backspace gesture based on the distance and time
         */
        public bool checkBackspaceGesture()
        {
            // return false;
            if (_queue.Count == 0)
            {
                Console.WriteLine("[Error] checkBackspaceGesture(): The queue is empty.");
                return false;
            }
            else
            {
                HandPoint hpFirst = _queue.First(), hpLast = _queue.Last();
                bool isBackspace = false;

                if (hpFirst.getX() - hpLast.getX() > BACK_THRE)
                {
                    isBackspace = true;
                }
                return isBackspace;
            }
        }

        public bool checkEnterGesture()
        {
            // return false;
            if (_queue.Count == 0)
            {
                Console.WriteLine("[Error] checkEnterGesture(): The queue is empty.");
                return false;
            }
            else
            {
                HandPoint hpFirst = _queue.First(), hpLast = _queue.Last();
                bool isEnter = false;

                if (hpLast.getX() - hpFirst.getX() > ENTER_THRE)
                {
                    isEnter = true;
                }
                return isEnter;
            }
        }

        public bool checkTyping()
        {
            if (_queue.Count == 0)
            {
                Console.WriteLine("[Error] checkTyping(): The queue is empty.");
                return false;
            }
            else
            {
                bool isTyping = false;
                if (_queue.Last().getTime() - _startTime < TOUCH_TIME_LIMIT)
                {
                    isTyping = true;
                }
                return isTyping;
            }
        }
    }
}
