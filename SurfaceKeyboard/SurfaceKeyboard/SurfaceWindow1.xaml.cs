using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private bool            isStart;
        private DateTime        startTime;
        private List<HandPoint> handPoints = new List<HandPoint>();

        private int             taskNo;
        private string[]        taskTexts;

        private const int       borderY = 80;

        private bool            isMouse = false;

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
        }

        private void updateTaskText()
        {
            // Select next text for the textblock
            int textSize = taskTexts.Length;
            TaskTextBlk.Text = taskTexts[taskNo % textSize];
        }

        private void Canvas_TouchDown(object sender, TouchEventArgs e)
        {
            // Get touchdown position
            Point touchPos = e.TouchDevice.GetPosition(this);

            if (touchPos.Y > borderY)
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



                // Show the information
                StatusTextBlk.Text = String.Format("({0}) X:{1}, Y:{2}, Time:{3}", handPoints.Count, touchPos.X, touchPos.Y, timeStamp);

                // Save the information
                handPoints.Add(new HandPoint(touchPos.X, touchPos.Y, timeStamp));
            }
        }

        private void SaveBtn_TouchDown(object sender, TouchEventArgs e)
        {
            // Save the touchdown seq to file
            string fPath = Directory.GetCurrentDirectory() + '\\';
            string fName = String.Format("{0:MM-dd_hh_mm_ss}", DateTime.Now) + ".csv";
            StatusTextBlk.Text = fPath + fName;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fName, true))
            {
                file.WriteLine("X, Y, Time");
                foreach (HandPoint point in handPoints)
                {
                    file.WriteLine(point.ToString());
                }
            }

            // Clear the timer and storage
            isStart = false;
            handPoints.Clear();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                StatusTextBlk.Text = String.Format("({0}) X:{1}, Y:{2}, Time:{3}", handPoints.Count, touchPos.X, touchPos.Y, timeStamp);

                // Save the information
                handPoints.Add(new HandPoint(touchPos.X, touchPos.Y, timeStamp));
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                // Save the touchdown seq to file
                string fPath = Directory.GetCurrentDirectory() + '\\';
                string fName = String.Format("{0:MM-dd_hh_mm_ss}", DateTime.Now) + ".csv";
                StatusTextBlk.Text = fPath + fName;

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fName, true))
                {
                    file.WriteLine("X, Y, Time");
                    foreach (HandPoint point in handPoints)
                    {
                        file.WriteLine(point.ToString());
                    }
                }

                // Clear the timer and storage
                isStart = false;
                handPoints.Clear();
            }
        }

        private void NextBtn_TouchDown(object sender, TouchEventArgs e)
        {
            taskNo++;
            updateTaskText();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                taskNo++;
                updateTaskText();
            }
        }
    }
}