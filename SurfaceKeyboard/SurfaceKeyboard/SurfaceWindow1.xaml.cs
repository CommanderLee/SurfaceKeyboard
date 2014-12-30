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
        private bool            isStart;
        private DateTime        startTime;

        // Number of the hand points and the list to store them
        private int                 hpNo;
        private List<HandPoint>     handPoints = new List<HandPoint>();

        // Number of the task texts, its size, and the array
        private int                 taskNo;
        private int                 taskSize;
        private string[]            taskTexts;

        // Constants for the gesture
        // A gesture will be triggered if it occured within 500 ms.
        private const double        MOVE_TIME_LIMIT = 500;
        // Threshold for backspace and enter
        private const double        BACK_THRE = 50;
        private const double        ENTER_THRE = 50;

        // Variables about the gesture
        // The queue of the movements
        private Queue<HandPoint>    movement = new Queue<HandPoint>();

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
            movement.Clear();

            loadTaksText();
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

        private void loadTaksText()
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
            Regex rgx = new Regex(@"[^\s]");
            string typeText = rgx.Replace(currText, "*");
            if (hpNo <= typeText.Length)
                typeText = typeText.Substring(0, hpNo) + "_";
            TaskTextBlk.Text = currText + "\n" + typeText;
        }

        private bool checkBackspaceGesture()
        {
            return true;
        }

        private bool checkEnterGesture()
        {
            return false;
        }

        private void InputCanvas_TouchDown(object sender, TouchEventArgs e)
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

            // Get touchdown position
            Point touchPos = e.TouchDevice.GetPosition(this);

            // Show the information
            StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2}) X:{3}, Y:{4}, Time:{5}", 
                taskNo + 1, taskSize, handPoints.Count, touchPos.X, touchPos.Y, timeStamp);

            // Save the information
            handPoints.Add(new HandPoint(touchPos.X, touchPos.Y, timeStamp, taskNo + "-" + hpNo));
            hpNo++;
            updateTaskText();
        }

        private void SaveBtn_TouchDown(object sender, TouchEventArgs e)
        {
            // Save the touchdown seq to file
            string fPath = Directory.GetCurrentDirectory() + '\\';
            string fName = String.Format("{0:MM-dd_HH_mm_ss}", DateTime.Now) + ".csv";
            StatusTextBlk.Text = fPath + fName;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fName, true))
            {
                file.WriteLine("X, Y, Time, TaskNo-PointNo");
                foreach (HandPoint point in handPoints)
                {
                    file.WriteLine(point.ToString());
                }
            }

            // Clear the timer and storage
            isStart = false;
            handPoints.Clear();
        }

        private void InputCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isMouse)
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

                // Get touchdown position
                Point touchPos = e.GetPosition(this);

                // Show the information
                StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2}) X:{3}, Y:{4}, Time:{5}",
                    taskNo + 1, taskSize, handPoints.Count, touchPos.X, touchPos.Y, timeStamp);

                // Save the information
                handPoints.Add(new HandPoint(touchPos.X, touchPos.Y, timeStamp, taskNo + "-" + hpNo));
                hpNo++;
                updateTaskText();
            }
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

        private void InputCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            /**
             * Gesture Handler
             * 1. Push the new point into the queue.
             * 2. Check if the finger move for certain threshold within time limit
             * 2.1 If satisfied, clear the queue and call the function
             * 2.2 If not, go to the next gesture
             * - Zhen. Dec.30th, 2014.
             * */
            // Push to the movement queue
            Point touchPos = e.TouchDevice.GetPosition(this);
            double timeStamp = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            movement.Enqueue(new HandPoint(touchPos.X, touchPos.Y, timeStamp, taskNo + "-" + hpNo));

            // Check: Time and Distance
            // TODO: Check Time
            if (checkBackspaceGesture())
            {
                // Delete one character (or one word)
                if (--hpNo < 0)
                    hpNo = 0;
                updateTaskText();
            }
            else if (checkEnterGesture())
            {
                // Output 'Enter' if applicable

                gotoNextText();
            }
        }
    }
}