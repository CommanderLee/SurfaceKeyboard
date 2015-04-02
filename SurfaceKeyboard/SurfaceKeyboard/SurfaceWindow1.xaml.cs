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
using System.Collections;
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

        // Number of the hand points and the list to store them.
        private int                 hpNo;
        private List<HandPoint>     handPoints = new List<HandPoint>();

        // Valid points: the touch point of each char.
        private List<HandPoint>     currValidPoints = new List<HandPoint>();
        private List<HandPoint>     validPoints = new List<HandPoint>();

        // Number of the task texts, its size, and its content
        private int                 taskNo;
        private int                 taskSize;
        private string[]            taskTexts;

        // Id -> GesturePoints Queue
        Hashtable                   movement = new Hashtable();

        // Show the soft keyboard on the screen (default: close)
        private bool                showKeyboard = false;
        ImageBrush                  keyboardOpen, keyboardClose;

        // Calibrate before each test sentence
        enum CalibStatus { Off, Preparing, Calibrating, Waiting, Done };
        private const int           CALIB_FINGER_NUM = 10;
        private const double        CALIB_MIN_TIME = 500;

        private CalibStatus         calibStatus = CalibStatus.Off;
        ImageBrush                  calibOn, calibOff;
        DateTime                    calibStartTime, calibEndTime;
        private List<HandPoint>     calibPoints = new List<HandPoint>();

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
            currValidPoints.Clear();
            showKeyboard = false;

            // Keyboard Control Button Image
            keyboardOpen = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_open.png")));
            keyboardClose = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_close.png")));
            KeyboardBtn.Background = keyboardClose;

            // Keyboard Image
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_1x.png");
            bitmap.EndInit();
            imgKeyboard.Source = bitmap;
            imgKeyboard.Visibility = Visibility.Hidden;

            // Calibration Control Button Image
            calibOn = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/hand_calibration_on.png")));
            calibOff = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/hand_calibration_off.png")));
            CalibBtn.Background = calibOff;

            loadTaskTexts();
            updateStatusBlock();
            updateTaskTextBlk();
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

        private void updateTaskTextBlk()
        {
            // Select next text for the textblock
            string currText = taskTexts[taskNo % taskSize];

            // Show asterisk feedback for the user
            Regex rgx = new Regex(@"[^\s]");
            string typeText = rgx.Replace(currText, "*");

            if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
            {
                if (hpNo <= typeText.Length)
                    typeText = typeText.Substring(0, hpNo) + "_";
                TaskTextBlk.Text = currText + "\n" + typeText;
            }
            else
            {
                TaskTextBlk.Text = typeText;
                //TaskTextBlk
            }
        }

        /**
         * 1. The user touch the screen ( type a key )
         * 2. Call 'saveTouchPoints' function: save the point in the 'handPoints' List
         * 3. Create hash table key for this point id.
         * 4. When the user lift his finger, call 'showTouchPoints' function: Give feedback, 
         * or: If it is a swipe ( gesture ).
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
            HandPoint touchPoint = new HandPoint(x, y, timeStamp, taskNo + "-" + hpNo + "-" + id, HPType.Touch);
            handPoints.Add(touchPoint);

            // Create elements in the hashtable
            if (!movement.ContainsKey(id))
            {
                GesturePoints myPoints = new GesturePoints(touchPoint, HandStatus.Type);
                // myPoints.Add(touchPoint);
                movement.Add(id, myPoints);
            }
            else
            {
                Debug.WriteLine("[Error] saveTouchPoints(): This Touchpoint ID alrealy exists: " + id);
            }
        }

        private void updateStatusBlock()
        {
            if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
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
            else
            {
                StatusTextBlk.Text = String.Format("Calibrating: {0}/{1} fingers detected.", calibPoints.Count, CALIB_FINGER_NUM);
            }
        }

        // Calibrate the hand position
        private void calibrateHands(double x, double y, int id, HPType pointType)
        {
            // New finger detected
            if (pointType == HPType.Touch)
            {
                switch (calibStatus)
                {
                    case CalibStatus.Preparing:
                        calibStatus = CalibStatus.Calibrating;
                        calibStartTime = DateTime.Now;
                        calibPoints.Add(new HandPoint(x, y, 0,
                            taskNo + "-" + "-1" + "-" + id, HPType.Calibrate));
                        break;

                    case CalibStatus.Calibrating:
                        calibPoints.Add(new HandPoint(x, y, DateTime.Now.Subtract(calibStartTime).TotalMilliseconds,
                            taskNo + "-" + "-1" + "-" + id, HPType.Calibrate));

                        // If we get enough fingers
                        if (calibPoints.Count == CALIB_FINGER_NUM)
                        {
                            calibStatus = CalibStatus.Waiting;
                            calibEndTime = DateTime.Now;
                        }
                        break;

                    default:
                        Debug.Write("Error: Unexpected value of 'calibStatus'. Maybe too many fingers.");
                        break;
                }
            }
            else
            {
                switch (calibStatus)
                {
                    case CalibStatus.Waiting:
                        if (DateTime.Now.Subtract(calibEndTime).TotalMilliseconds >= CALIB_MIN_TIME)
                        {
                            calibStatus = CalibStatus.Done;
                            // Save to variables
                            // TODO: Save to some models

                            // Save to currValidPoints (output file). Id: 'taskNo' - '-1' - '0~9'. 
                            // TODO: Distinguish them. 0:left litte finger, 4:left thumb, 5:right thumb, 9:right little finger, and so on. Refer to the standard hand position.
                            currValidPoints.AddRange(calibPoints);

                            calibPoints.Clear();
                        }
                        break;

                    default:
                        Debug.Write("Error: Unexpected value of 'calibStatus'");
                        break;
                }
            }

            updateStatusBlock();
            updateTaskTextBlk();
        }

        private void InputCanvas_TouchDown(object sender, TouchEventArgs e)
        {
            // Get touchdown position
            if (e.TouchDevice.GetIsFingerRecognized())
            {
                Point touchPos = e.TouchDevice.GetPosition(this);

                // No calibration, or the user has done his calibration
                if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                {
                    saveTouchPoints(touchPos.X, touchPos.Y, e.TouchDevice.Id);
                }
                else // Calibration points
                {
                    calibrateHands(touchPos.X, touchPos.Y, e.TouchDevice.Id, HPType.Touch);
                }
            }
        }

        private void InputCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isMouse)
            {
                // Get touchdown position
                Point touchPos = e.GetPosition(this);

                saveTouchPoints(touchPos.X, touchPos.Y, -1);
                // No calibration, or the user has done his calibration
                if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                {
                    saveTouchPoints(touchPos.X, touchPos.Y, -1);
                }
                else // Calibration points
                {
                    calibrateHands(touchPos.X, touchPos.Y, -1, HPType.Touch);
                }
            }
        }

        private string getTestingTag()
        {
            // Return the tag string for testing status( with/without keyboard .etc)
            string myTag = "";

            // Keyboard
            if (showKeyboard)
                myTag += "_KbdOn";
            else
                myTag += "_KbdOff";

            if (calibStatus == CalibStatus.Off)
                myTag += "_CalibOff";
            else
                myTag += "_CalibOn";

            return myTag;
        }

        private void SaveBtn_TouchDown(object sender, TouchEventArgs e)
        {
            // Save the touchdown seq to file
            string fPath = Directory.GetCurrentDirectory() + '\\';
            string fName = String.Format("{0:MM-dd_HH_mm_ss}", DateTime.Now) + getTestingTag() + ".csv";
            string fNameTyping = String.Format("{0:MM-dd_HH_mm_ss}", DateTime.Now) + getTestingTag() + "_typing.csv";
            StatusTextBlk.Text = fPath + fName;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fName, true))
            {
                file.WriteLine("X, Y, Time, TaskNo-PointNo-FingerId, PointType");
                foreach (HandPoint point in handPoints)
                {
                    file.WriteLine(point.ToString());
                }
            }

            if (currValidPoints.Count > 0)
            {
                validPoints.AddRange(currValidPoints);
            }
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fNameTyping, true))
            {
                file.WriteLine("X, Y, Time, TaskNo-PointNo-FingerId, PointType");
                foreach (HandPoint point in validPoints)
                {
                    file.WriteLine(point.ToString());
                }
            }

            // Clear the timer and storage
            isStart = false;
            handPoints.Clear();
            validPoints.Clear();
            currValidPoints.Clear();
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

            validPoints.AddRange(currValidPoints);
            currValidPoints.Clear();

            // Clear the status if the calibration mode is ON
            if (calibStatus != CalibStatus.Off)
            {
                calibStatus = CalibStatus.Preparing;
                // TODO: Change the color or hint or something
            }

            updateStatusBlock();
            updateTaskTextBlk();
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

        /**
        * Gesture Handler - Zhen. 
        * Update: Jan. 29th 2014.
        * 1. Get the queue with the id.
        * 2. Call the function of GesturePoints Class
        * 3. Complete gestures based on the return value
        * */
        private void handleGesture(double x, double y, int id)
        {
            // Push to the movement queue and check time
            double timeStamp = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            HandPoint movePoint = new HandPoint(x, y, timeStamp, taskNo + "-" + hpNo + "-" + id, HPType.Move);
            if (movement.ContainsKey(id))
            {
                GesturePoints myPoints = (GesturePoints)movement[id];
                myPoints.Add(movePoint);
                handPoints.Add(movePoint);

                // Check Distance
                if (myPoints.checkBackspaceGesture())
                {
                    // Delete ONE character
                    // TODO: Delete one word?
                    if (myPoints.getStatus() != HandStatus.Backspace)
                    {
                        myPoints.setStatus(HandStatus.Backspace);
                        if (hpNo > 0)
                        {
                            hpNo--;
                        }
                        updateTaskTextBlk();
                        // Debug.WriteLine("do Backspace");
                    }
                }
                else if (myPoints.checkEnterGesture())
                {
                    if (myPoints.getStatus() != HandStatus.Enter)
                    {
                        myPoints.setStatus(HandStatus.Enter);
                        // TODO: Output 'Enter' if applicable
                        // Show the next task
                        gotoNextText();
                    }
                }
            }
            else
            {
                Debug.WriteLine("[Error] handleGesture(): id not exist." + id);
            }
        }

        private void InputCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice.GetIsFingerRecognized())
            {
                Point touchPos = e.TouchDevice.GetPosition(this); 
                if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                {
                    handleGesture(touchPos.X, touchPos.Y, e.TouchDevice.Id);
                }
                else
                {
                    calibrateHands(touchPos.X, touchPos.Y, e.TouchDevice.Id, HPType.Move);
                }
            }
        }

        private void InputCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouse)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    Point touchPos = e.GetPosition(this);
                    if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                    {
                        handleGesture(touchPos.X, touchPos.Y, -1);
                    }
                    else
                    {
                        calibrateHands(touchPos.X, touchPos.Y, -1, HPType.Move);
                    }
                }
            }
        }

        private void releaseGesture(int id)
        {
            if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
            {
                if (movement.ContainsKey(id))
                {
                    GesturePoints myPoints = (GesturePoints)movement[id];
                    switch (myPoints.getStatus())
                    {
                        case HandStatus.Away:
                            break;
                        case HandStatus.Backspace:
                            break;
                        case HandStatus.Enter:
                            break;
                        case HandStatus.Rest:
                            break;
                        case HandStatus.Type:
                            if (myPoints.checkTyping())
                            {
                                currValidPoints.Add(myPoints.getStartPoint());

                                hpNo++;
                                updateStatusBlock();
                                updateTaskTextBlk();
                            }
                            break;
                    }
                    movement.Remove(id);
                }
                else
                {
                    Debug.WriteLine("[Error] releaseGesture(): id not exist." + id);
                }
            }
            else
            {
                // Reset calibration points
                calibStatus = CalibStatus.Preparing;
                calibPoints.Clear();
                updateStatusBlock();
            }
        }

        private void InputCanvas_TouchUp(object sender, TouchEventArgs e)
        {
            releaseGesture(e.TouchDevice.Id);
        }

        private void InputCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            releaseGesture(-1);
        }

        private void ClearBtn_TouchDown(object sender, TouchEventArgs e)
        {
            // Clear the touch points of this sentence
            // Also delete the calibration points in the list
            currValidPoints.Clear();
            hpNo = 0;
            updateStatusBlock();
            updateTaskTextBlk();

            if (calibStatus != CalibStatus.Off)
            {
                calibStatus = CalibStatus.Preparing;
                // TODO: Change the color or hint or something
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                ClearBtn_TouchDown(null, null);
            }
        }

        private void KeyboardBtn_TouchDown(object sender, TouchEventArgs e)
        {
            showKeyboard = !showKeyboard;
            if (showKeyboard)
            {
                KeyboardBtn.Background = keyboardOpen;
                imgKeyboard.Visibility = Visibility.Visible;
            }
            else
            {
                KeyboardBtn.Background = keyboardClose;
                imgKeyboard.Visibility = Visibility.Hidden;
            }
        }

        private void KeyboardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                KeyboardBtn_TouchDown(null, null);
            }
        }

        private void CalibBtn_TouchDown(object sender, TouchEventArgs e)
        {
            if (calibStatus == CalibStatus.Off)
            {
                calibStatus = CalibStatus.Preparing;
                CalibBtn.Background = calibOn;
            }
            else
            {
                calibStatus = CalibStatus.Off;
                CalibBtn.Background = calibOff;
            }
            updateStatusBlock();
            updateTaskTextBlk();
        }

        private void CalibBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                CalibBtn_TouchDown(null, null);
            }
        }

        private void DeleteBtn_TouchDown(object sender, TouchEventArgs e)
        {
            // Delete one word. Now hpNo points to the position of next input char.
            string currText = taskTexts[taskNo % taskSize];
            int removeStart = hpNo - 1;
            // Delete at least one character
            if (removeStart >= 0)
            {
                for (; removeStart > 0; --removeStart)
                {
                    if (currText[removeStart - 1] == ' ')
                        break;
                }
                currValidPoints.RemoveRange(removeStart, hpNo - removeStart);
                hpNo = removeStart;
            }

            updateStatusBlock();
            updateTaskTextBlk();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMouse)
            {
                DeleteBtn_TouchDown(null, null);
            }
        }

    }
}