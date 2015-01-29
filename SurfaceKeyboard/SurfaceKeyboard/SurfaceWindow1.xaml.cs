using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;

namespace SurfaceKeyboard
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        // The start time of the app
        private bool                isStart;
        private DateTime            startTime;

        // Number of the hand points and the list to store them
        private int                 hpNo;
        private List<HandPoint>     handPoints = new List<HandPoint>();

        // Number of the task texts, its size, and its content
        private int                 taskNo;
        private int                 taskSize;
        private string[]            taskTexts;

        // Constants for the gesture
        // A gesture will be triggered if it occured within the time limit
        private const double        MOVE_TIME_LIMIT = 100;
        // Length threshold (pixels) for backspace and enter
        private const double        BACK_THRE = 100;
        private const double        ENTER_THRE = 100;

        // Variables for the gesture
        // The queue of the movements
        private Queue<HandPoint>    movement = new Queue<HandPoint>();
        enum HandStatus { Away, Backspace, Enter, Type, Rest };
        HandStatus                  handStatus;

        // Mark true if using mouse instead of fingers
        private bool                isMouse = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            InitializeComponent();

            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();

            isStart = false;
            taskNo = 0;
            hpNo = 0;
            handStatus = HandStatus.Away;
            movement.Clear();

            loadTaskTexts();
            updateTaskText();
        }

        /// <summary>
        /// Occurs when the window is about to close. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Remove handlers for window availability events
            RemoveWindowAvailabilityHandlers();
        }

        /// <summary>
        /// Adds handlers for window availability events.
        /// </summary>
        private void AddWindowAvailabilityHandlers()
        {
            // Subscribe to surface window availability events
            ApplicationServices.WindowInteractive += OnWindowInteractive;
            ApplicationServices.WindowNoninteractive += OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable += OnWindowUnavailable;
        }

        /// <summary>
        /// Removes handlers for window availability events.
        /// </summary>
        private void RemoveWindowAvailabilityHandlers()
        {
            // Unsubscribe from surface window availability events
            ApplicationServices.WindowInteractive -= OnWindowInteractive;
            ApplicationServices.WindowNoninteractive -= OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable -= OnWindowUnavailable;
        }

        /// <summary>
        /// This is called when the user can interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowInteractive(object sender, EventArgs e)
        {
            //TODO: enable audio, animations here
        }

        /// <summary>
        /// This is called when the user can see but not interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowNoninteractive(object sender, EventArgs e)
        {
            //TODO: Disable audio here if it is enabled

            //TODO: optionally enable animations here
        }

        /// <summary>
        /// This is called when the application's window is not visible or interactive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowUnavailable(object sender, EventArgs e)
        {
            //TODO: disable audio, animations here
        }

        private void loadTaskTexts()
        {
            // Load task text from file
            string fPath = Directory.GetCurrentDirectory() + "\\";
            string fName = "TaskText.txt";
            taskTexts = System.IO.File.ReadAllLines(fPath + fName);
            taskSize = taskTexts.Length;
        }

        private void updateTaskText()
        {
            // Select next text for the textblock
            string currText = taskTexts[taskNo % taskSize];
            // Show asterisk feedback for the user
            Regex rgx = new Regex(@"[^\s]");
            string typeText = rgx.Replace(currText, "*");
            if (hpNo <= typeText.Length)
                typeText = typeText.Substring(0, hpNo) + "_";
            TaskTextBlk.Text = currText + "\n" + typeText;
        }

        private bool checkBackspaceGesture()
        {
            Debug.WriteLine(String.Format("{0}:[0]:{1}, [last]:{2}, status:{3}", 
                movement.Count, movement.First(), movement.Last(), handStatus));
            HandPoint hpFirst = movement.First(), hpLast = movement.Last();
            bool isBackspace = false;

            if (hpFirst.getX() - hpLast.getX() > BACK_THRE)
            {
                isBackspace = true;
                // Debug.WriteLine("is Backspace");
            }
            return false;
            // return isBackspace;
        }

        private bool checkEnterGesture()
        {
            Debug.WriteLine(String.Format("{0}:[0]:{1}, [last]:{2}, status:{3}",
                movement.Count, movement.First(), movement.Last(), handStatus));
            HandPoint hpFirst = movement.First(), hpLast = movement.Last();
            bool isEnter = false;

            if (hpLast.getX() - hpFirst.getX() > ENTER_THRE)
            {
                isEnter = true;
            }
            return false;
            // return isEnter;
        }

        /**
         * 1. The user touch the screen ( type a key )
         * 2. Call 'saveTouchPoints' function: save the point in the 'handPoints' List
         * 3. When the user lift his finger, call 'showTouchPoints' function: Give feedback, 
         * or: If it is a swipe ( gesture ). Decide this in the '**up' function.
         */
        private void saveTouchPoints(double x, double y, int id)
        {
            // Get touchdown time
            double timeStamp = 0;
            if (!isStart)
            {
                isStart = true;
                startTime = DateTime.Now;
                timeStamp = 0;
            }
            else
            {
                timeStamp = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            }

            // Save the information
            handPoints.Add(new HandPoint(x, y, timeStamp, taskNo + "-" + hpNo + "-" + id, HPType.Touch));

            // Set the status ( assumption )
            handStatus = HandStatus.Type;
        }

        private void showTouchInfo()
        {
            if (hpNo > 0)
            {
                HandPoint hpLast = handPoints.Last();
                // Show the information
                StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2}) X:{3}, Y:{4}, Time:{5}, ID:{6}",
                    taskNo + 1, taskSize, hpNo, hpLast.getX(), hpLast.getY(), hpLast.getTime(), hpLast.getId());
            }
            else
            {
                StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2}) X:{3}, Y:{4}, Time:{5}",
                    taskNo + 1, taskSize, "N/A", "N/A", "N/A", "N/A");
            }
        }

        private void InputCanvas_TouchDown(object sender, TouchEventArgs e)
        {
            // Get touchdown position
            if (e.TouchDevice.GetIsFingerRecognized())
            {
                Point touchPos = e.TouchDevice.GetPosition(this);
                saveTouchPoints(touchPos.X, touchPos.Y, e.TouchDevice.Id);
            }
        }

        private void InputCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isMouse)
            {
                // Get touchdown position
                Point touchPos = e.GetPosition(this);

                saveTouchPoints(touchPos.X, touchPos.Y, -1);
            }
        }

        private void SaveBtn_TouchDown(object sender, TouchEventArgs e)
        {
            // Save the touchdown seq to file
            string fPath = Directory.GetCurrentDirectory() + '\\';
            string fName = String.Format("{0:MM-dd_HH_mm_ss}", DateTime.Now) + ".csv";
            StatusTextBlk.Text = fPath + fName;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fName, true))
            {
                file.WriteLine("X, Y, Time, TaskNo-PointNo-FingerId, PointType");
                foreach (HandPoint point in handPoints)
                {
                    file.WriteLine(point.ToString());
                }
            }

            // Clear the timer and storage
            isStart = false;
            handPoints.Clear();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                SaveBtn_TouchDown(null, null);
            }
        }

        // Call when press the next button or do the 'next' gesture
        private void gotoNextText()
        {
            taskNo++;
            hpNo = 0;
            showTouchInfo();
            updateTaskText();
            movement.Clear();
        }

        private void NextBtn_TouchDown(object sender, TouchEventArgs e)
        {
            gotoNextText();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                gotoNextText();
            }
        }

        private void handleGesture(double x, double y, int id)
        {
            /**
             * Gesture Handler - Zhen. Dec.30th, 2014.
             * 1. Push the new point into the queue.
             * 2. Check if the finger movement is larger than the threshold within the time limit
             * 2.1 If satisfied, clear the queue and call the function
             * 2.2 If not, go to the next gesture
             * */
            // Push to the movement queue and check time
            double timeStamp = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            while (movement.Count > 0 && timeStamp - movement.Peek().getTime() > MOVE_TIME_LIMIT)
            {
                movement.Dequeue();
            }
            // Temporarily add this point to the handpoint
            HandPoint movePoint = new HandPoint(x, y, timeStamp, taskNo + "-" + hpNo + "-" + id, HPType.Move);
            handPoints.Add(movePoint);
            movement.Enqueue(movePoint);
            //movement.Enqueue(new HandPoint(x, y, timeStamp, taskNo + "-" + hpNo + "-" + id));

            // Check Distance
            if (checkBackspaceGesture())
            {
                // Delete ONE character
                // TODO: Delete one word?
                if (handStatus != HandStatus.Backspace)
                {
                    handStatus = HandStatus.Backspace;
                    if (hpNo > 0)
                    {
                        hpNo--;
                        handPoints.RemoveAt(handPoints.Count - 1);
                    }
                    updateTaskText();
                    movement.Clear();
                    // Debug.WriteLine("do Backspace");
                }
            }
            else if (checkEnterGesture())
            {
                if (handStatus != HandStatus.Enter)
                {
                    handStatus = HandStatus.Enter;
                    if (hpNo > 0)
                    {
                        hpNo--;
                        handPoints.RemoveAt(handPoints.Count - 1);
                    }
                    // TODO: Output 'Enter' if applicable
                    // Show the next task
                    gotoNextText();
                    movement.Clear();
                }
            }
        }

        private void InputCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice.GetIsFingerRecognized())
            {
                Point touchPos = e.TouchDevice.GetPosition(this);
                handleGesture(touchPos.X, touchPos.Y, e.TouchDevice.Id);
            }
        }

        private void InputCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouse)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    Point touchPos = e.GetPosition(this);
                    handleGesture(touchPos.X, touchPos.Y, -1);
                }
            }
        }

        private void releaseGesture()
        {
            switch (handStatus)
            {
                case HandStatus.Away:
                    break;
                case HandStatus.Backspace:
                    handStatus = HandStatus.Away;
                    break;
                case HandStatus.Enter:
                    handStatus = HandStatus.Away;
                    break;
                case HandStatus.Rest:
                    break;
                case HandStatus.Type:
                    hpNo++;
                    showTouchInfo();
                    updateTaskText();
                    handStatus = HandStatus.Away;
                    break;
            }
        }

        private void InputCanvas_TouchUp(object sender, TouchEventArgs e)
        {
            releaseGesture();
        }

        private void InputCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            releaseGesture();
        }

    }
}