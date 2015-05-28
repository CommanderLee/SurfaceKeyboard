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
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Windows.Threading;
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
        // The window to collect user id 
        WindowUserId        userIdWindow;
        string              userId;

        // Number of deletions
        int                 deleteNum;

        // Number of the hand points and the list to store them 
        // hpNo: number of current hp. Backspacing when deleted.
        // hpIndex: the real index of the points. always incresing(hand) or decresing(mouse)
        int                 hpNo, hpIndex;
        List<HandPoint>     handPoints = new List<HandPoint>();
        // hpNo: [0, ...) normal points. Others: strings. 
        const string        hpNoCalibPnt = "CALIB";
        const string        hpNoCenterPnt = "CENTER";

        // Valid points: the touch point of each char.
        List<HandPoint>     validPoints = new List<HandPoint>();
        // Gesture: Type, Enter, Backspace .etc. Move into validPoints after 'Next'
        List<GesturePoints> currGestures = new List<GesturePoints>();

        // Index of the task texts, its size, and its content 
        int                 taskIndex;
        int                 taskSize;
        string[]            taskTexts;

        // Show the soft keyboard on the screen (default: close) 
        ImageBrush          kbdBtnImgOn, kbdBtnImgOff;
        enum KbdImgStatus   { SurfSize, PhySize, Off };
        KbdImgStatus        kbdImgStatus;
        BitmapImage[]       kbdImages;
        double              kbdWidth, kbdHeight;

        // Calibrate before each test sentence 
        enum CalibStatus    { Off, Preparing, Calibrating, Waiting, Done };
        CalibStatus         calibStatus = CalibStatus.Off;
        List<HandPoint>     calibPoints = new List<HandPoint>();

        // Calibration time threshold and timer 
        DateTime            calibStartTime, calibEndTime;
        const double        CALIB_WAITING_TIME = 500;

        // Calibration Button image 
        ImageBrush          calibBtnImgOn, calibBtnImgOff;

        // Hand Model for calibration 
        HandModel           userHand = new HandModel();

        // Constants for recovering missing points
        const int           TYPING_DIST_THRE = 30;

        // Different devices: 
        // Hand for touch typing, Physical Keyboard for normal typing test, Mouse for debug on laptop 
        enum InputDevice    { Hand, PhyKbd, Mouse };
        InputDevice         currDevice;

        // Physical keyboard test 
        string              currTyping;
        List<string>        phyStrings = new List<string>();
        bool                isTypingStart;

        // typingTime: ms 
        DateTime            typingStartTime;
        double              typingTime;

        bool                circleControl = true;
        // Circle bias
        const int           circleBiasY = 50;
        
        // Size of debug circles
        const int           touchCircleSize = 20;
        const int           moveCircleSize = 5;
        const int           releaseCircleSize = 10;
        const int           recoverCircleSize = 8;
        const int           circleThickness = 1;

        // Different colors for debug circles
        Brush               touchCircleBrush;
        Brush               moveCircleNearBrush;
        Brush               moveCircleFarBrush;
        Brush               releaseCircleBrush;
        Brush               recoverCircleBrush;

        List<Ellipse>       circleList = new List<Ellipse>();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            InitializeComponent();

            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();

            currDevice = InputDevice.PhyKbd;

            //isStart = false;
            deleteNum = 0;
            taskIndex = 0;
            hpIndex = -1;
            hpNo = 0;

            touchCircleBrush = Brushes.Red;
            moveCircleNearBrush = Brushes.Yellow;
            moveCircleFarBrush = Brushes.White;
            releaseCircleBrush = Brushes.Blue;
            recoverCircleBrush = Brushes.Magenta;

            //currValidPoints.Clear();
            currTyping = "";
            isTypingStart = false;

            kbdImgStatus = KbdImgStatus.Off;
            // Keyboard Control Button Image 
            kbdBtnImgOn = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_open.png")));
            kbdBtnImgOff = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_close.png")));
            KeyboardBtn.Background = kbdBtnImgOff;
            KeyboardBtn.Content = "";

            BackspaceBtn.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/backspace.png")));
            BackspaceBtn.Content = "";

            // Keyboard Image (same as, or similar to physical keyboard) 
            kbdImages = new BitmapImage[2];

            // Surface Keyboard Image 
            int statusId = (int)KbdImgStatus.SurfSize;
            kbdImages[statusId] = new BitmapImage();
            kbdImages[statusId].BeginInit();
            kbdImages[statusId].UriSource = new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_1_23x_surface.png");
            kbdImages[statusId].EndInit();

            // Physical Keyboard Image 
            statusId = (int)KbdImgStatus.PhySize;
            kbdImages[statusId] = new BitmapImage();
            kbdImages[statusId].BeginInit();
            kbdImages[statusId].UriSource = new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_1_02x_physical.png");
            kbdImages[statusId].EndInit();

            imgKeyboard.Visibility = Visibility.Hidden;
            
            // Calibration Control Button Image 
            calibBtnImgOn = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/hand_calibration_on.png")));
            calibBtnImgOff = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/hand_calibration_off.png")));
            CalibBtn.Background = calibBtnImgOff;

            loadTaskTexts();
            updateStatusBlock();
            updateTaskTextBlk();

            // Open a window to input the User ID 
            userIdWindow = new WindowUserId();
            userIdWindow.ShowActivated = true;
            userIdWindow.ShowDialog();
            userId = userIdWindow.getUserId();

            updateWindowTitle();
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

        /** Update UI Information **/

        /// <summary>
        /// There are various versions of code implementing Shuffle algorithm without bias. I don't want to focus on this topic. 
        /// My code is based on ShitalShah's answer from http://stackoverflow.com/a/22668974/4762924 
        /// </summary>
        private void swapTexts(int i, int j)
        {
            var temp = taskTexts[i];
            taskTexts[i] = taskTexts[j];
            taskTexts[j] = temp;
        }

        private void shuffleTexts(Random rnd)
        {
            for (int i = 0; i < taskTexts.Length; ++i)
                swapTexts(i, rnd.Next(i, taskTexts.Length));
        }

        private void loadTaskTexts()
        {
            // Load task text from file. Just name it 'TaskText.txt'.
            string fPath = Directory.GetCurrentDirectory() + "\\";
            string fName = "TaskText.txt";
            taskTexts = System.IO.File.ReadAllLines(fPath + fName);

            // Shuffle if necessary, and save the shuffled text later.
            shuffleTexts(new Random());

            taskSize = taskTexts.Length;
        }

        /// <summary>
        /// TaskTextBlock: Main block in the top-center of the window.
        /// 1st line: Original text, 2nd line: User's input 
        /// </summary>
        private void updateTaskTextBlk()
        {
            string currText = taskTexts[taskIndex % taskSize];

            // Show asterisk feedback for the user 
            Regex rgx = new Regex(@"[^\s]");
            string typeText = rgx.Replace(currText, "*");

            if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
            {
                if (hpNo >= 0 && hpNo <= typeText.Length)
                    typeText = typeText.Substring(0, hpNo) + "_";
                TaskTextBlk.Text = currText + "\n" + typeText;
            }
            else
            {
                // Use asterisk while calibrating 
                TaskTextBlk.Text = typeText;
            }
        }

        /// <summary>
        /// StatusBlock: Show some x,y,id.etc information on the top-left of the window
        /// </summary>
        private void updateStatusBlock()
        {
            if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
            {
                //int hpIndex = currGestures.Count - 1;
                switch (currDevice)
                {
                    case InputDevice.PhyKbd:
                        // Physical Keyboard 
                        StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2})",
                            taskIndex + 1, taskSize, hpIndex);
                        break;

                    case InputDevice.Hand:
                    case InputDevice.Mouse:
                        // Touch or Mouse(Debug), points > 0
                        if (hpNo > 0)
                        {
                            HandPoint hpLast = handPoints.Last();

                            StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2}) X:{3}, Y:{4}, Time:{5}, ID:{6}",
                                taskIndex + 1, taskSize, hpIndex, hpLast.getX(), hpLast.getY(), hpLast.getTime(), hpLast.getId());
                        }
                        // points == 0 or PhysicalKeyboard Mode
                        else
                        {
                            StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2}) X:{3}, Y:{4}, Time:{5}",
                                taskIndex + 1, taskSize, "N/A", "N/A", "N/A", "N/A");
                        }
                        break;
                }
            }
            else
            {
                StatusTextBlk.Text = String.Format("Calibrating: {0}/{1} fingers detected.", calibPoints.Count, HandModel.FINGER_NUMBER);
            }
        }

        /// <summary>
        /// Move the keyboard image to correct place  
        /// </summary>
        private void updateKeyboardImage()
        {
            Point kbdCenter = userHand.getCenterPt();
            if (kbdImgStatus != KbdImgStatus.Off)
            {
                Canvas.SetLeft(imgKeyboard, kbdCenter.X - kbdWidth / 2);
                Canvas.SetTop(imgKeyboard, kbdCenter.Y - kbdHeight / 2 - TaskTextBlk.ActualHeight);
            }
        }

        /// <summary>
        /// Update Main Window Title 
        /// </summary>
        private void updateWindowTitle()
        {
            this.Title = "Surface Keyboard - " + userId + " - " + currDevice;
        }

        /** Process Touch Point Information **/

        /// <summary>
        /// Calibrate the hand position 
        /// </summary>
        private void calibrateHands(double x, double y, int id, HPType pointType)
        {
            if (pointType == HPType.Touch)
            {
                // New finger detected 
                switch (calibStatus)
                {
                    case CalibStatus.Preparing:
                        calibStatus = CalibStatus.Calibrating;
                        calibStartTime = DateTime.Now;
                        calibPoints.Add(new HandPoint(x, y, 0,
                            taskIndex + "_" + hpNoCalibPnt + "_" + id, HPType.Calibrate));
                        break;

                    case CalibStatus.Calibrating:
                        calibPoints.Add(new HandPoint(x, y, DateTime.Now.Subtract(calibStartTime).TotalMilliseconds,
                            taskIndex + "_" + hpNoCalibPnt + "_" + id, HPType.Calibrate));

                        // If we get enough fingers 
                        if (calibPoints.Count == HandModel.FINGER_NUMBER)
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
                // Wait for some time 
                switch (calibStatus)
                {
                    case CalibStatus.Waiting:
                        if (DateTime.Now.Subtract(calibEndTime).TotalMilliseconds >= CALIB_WAITING_TIME)
                        {
                            calibStatus = CalibStatus.Done;

                            // Save to variables 
                            if (!userHand.loadHandPoints(calibPoints))
                                Debug.Write("Error: load hand points failed.");

                            // TODO: Push calibration point from userHand to validPoints after 'Next'
                            // currValidPoints.AddRange(userHand.getFingerPoints().ToList<HandPoint>());

                            // Show keyboard image at center position 
                            updateKeyboardImage();

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

        /// <summary>
        /// Find corresponding gesture points list
        /// </summary>
        /// <param name="touchId"></param>
        /// <returns></returns>
        private GesturePoints findGesturePoints(int touchId)
        {
            GesturePoints findPoints = null;

            // Find the same queue
            foreach (GesturePoints gPoint in currGestures)
            {
                string[] currHPId = gPoint.getStartPoint().getId().Split('_');
                if (currHPId.Length == 3)
                {
                    int currDeviceId = Convert.ToInt32(currHPId[2]);
                    if (currDeviceId == touchId)
                    {
                        findPoints = gPoint;
                        break;
                    }
                }
            }

            return findPoints;
        }

        /// <summary>
        /// Update the pointQueue(currGestures) with given hand point 
        /// </summary>
        /// <param name="touchPoint">New point detected</param>
        /// <param name="touchId">ID of the new point</param>
        /// <returns>If the ID exists, return gesturepoints</returns>
        private GesturePoints updateGesturePoints(HandPoint touchPoint, int touchId)
        {
            GesturePoints findPoints = findGesturePoints(touchId);

            if (findPoints != null)
            {
                findPoints.Add(touchPoint);
            }
            else
            {
                GesturePoints myPoints = new GesturePoints(touchPoint, HandStatus.Wait);
                currGestures.Add(myPoints);
            }

            return findPoints;
        }

        /// <summary>
        /// draw circles on the canvas (debugging)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void drawCircle(double x, double y, Brush fillBrush, int size)
        {
            Ellipse myEllipse = new Ellipse();
            myEllipse.Fill = fillBrush;
            myEllipse.Stroke = Brushes.Black;
            myEllipse.StrokeThickness = circleThickness;
            myEllipse.Width = myEllipse.Height = size;

            Point rawPoint = new Point(x, y);
            Point relativePoint = InputCanvas.PointFromScreen(rawPoint);
            Canvas.SetLeft(myEllipse, relativePoint.X - myEllipse.Width / 2);
            Canvas.SetTop(myEllipse, relativePoint.Y - myEllipse.Height / 2 + circleBiasY);
            InputCanvas.Children.Add(myEllipse);
            circleList.Add(myEllipse);
        }

        private void clearCircles()
        {
            foreach (Ellipse circle in circleList)
            {
                InputCanvas.Children.Remove(circle);
            }
            circleList.Clear();
        }

        /// <summary>
        /// Save the point to the 'handPoints' List.
        /// </summary>
        /// 1. The user touch the screen ( type a key ) 
        /// 2. Call 'saveTouchPoints' function: save the point in the 'handPoints' List 
        /// 3. Create hash table key for this point id. 
        /// 4. When the user lift his finger, call 'showTouchPoints' function: Give feedback, 
        private void saveTouchPoints(double x, double y, int id)
        {
            // Get touchdown time
            double timeStamp = 0;
            if (!isTypingStart)
            {
                isTypingStart = true;
                typingStartTime = DateTime.Now;
                timeStamp = 0;
            }
            else
            {
                timeStamp = DateTime.Now.Subtract(typingStartTime).TotalMilliseconds;
            }

            // Save the information 
            //int hpIndex = currGestures.Count;
            HandPoint touchPoint = new HandPoint(x, y, timeStamp, taskIndex + "_" + hpIndex + "_" + id, HPType.Touch);
            handPoints.Add(touchPoint);

            // Add new point(should return null because it is new)
            if (updateGesturePoints(touchPoint, id) == null)
            {
                // Real-time UI feedback. Note: Change its status later after released.
                ++hpNo;
                updateTaskTextBlk();
                updateStatusBlock();
            }
            else
            {
                Debug.WriteLine("[Error] saveTouchPoints(): This Touchpoint ID alrealy exists: " + id);
            }
        }

        /// <summary>
        /// Touch the main area (Input Canvas)  
        /// </summary>
        private void InputCanvas_TouchDown(object sender, TouchEventArgs e)
        {
            if (currDevice == InputDevice.Hand && e.TouchDevice.GetIsFingerRecognized())
            {
                Console.WriteLine("Hand Detected.");
                // Get touchdown position 
                Point touchPos = e.TouchDevice.GetPosition(this);

                // Calibration off, or the user has done his calibration 
                if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                {
                    ++hpIndex;
                    saveTouchPoints(touchPos.X, touchPos.Y, e.TouchDevice.Id);

                    // Draw on the canvas
                    if (circleControl)
                    {
                        drawCircle(touchPos.X, touchPos.Y, touchCircleBrush, touchCircleSize);
                    }
                }
                else
                {
                    calibrateHands(touchPos.X, touchPos.Y, e.TouchDevice.Id, HPType.Touch);
                }
            }
        }

        /// <summary>
        /// Left-click the main area (Input Canvas)  
        /// </summary>
        private void InputCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currDevice == InputDevice.Mouse)
            {
                Console.WriteLine("Mouse Detected.");
                // Get touchdown position 
                Point touchPos = e.GetPosition(this);
                //Point relativePos = Input.PointFromScreen(touchPos);
                //Console.WriteLine(String.Format("X:{0}, Y:{1}, Relative X:{2}, Y:{3}", touchPos.X, touchPos.Y, relativePos.X, relativePos.Y));
                // To avoid repetition. This is the first one
                int mouseTouchId = --hpIndex;
                // No calibration, or the user has done his calibration 
                if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                {
                    saveTouchPoints(touchPos.X, touchPos.Y, mouseTouchId);

                    // Draw on the canvas
                    if (circleControl)
                    {
                        drawCircle(touchPos.X, touchPos.Y, touchCircleBrush, touchCircleSize);
                    }
                }
                else
                {
                    calibrateHands(touchPos.X, touchPos.Y, mouseTouchId, HPType.Touch);
                }
            }
        }

        /// <summary>
        /// Typing with the physical keyboard 
        /// </summary>
        private void SurfaceWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (currDevice == InputDevice.PhyKbd)
            {
                string str = e.Key.ToString();
                Console.Write(e.Device + " " + str + "\n");

                if (!isTypingStart)
                {
                    typingStartTime = DateTime.Now;
                    isTypingStart = true;
                }
                else
                {
                    typingTime = DateTime.Now.Subtract(typingStartTime).TotalMilliseconds;
                }

                // A-Z 
                if (str.Length == 1)
                {
                    currTyping += str;
                    ++hpNo;
                }
                else if (str == "Space")
                {
                    currTyping += "_";
                    ++hpNo;
                }
                else if (str == "Return")
                {
                    gotoNextText();
                }
                else if (str == "Back")
                {
                    //deleteWord();
                    deleteCharacter();
                }

                updateTaskTextBlk();
                updateStatusBlock();
            }
        }

        /// <summary>
        /// Gesture Handler - Zhen. 
        /// </summary>
        /// 1. Get the queue with the id.
        /// 2. Call the function of GesturePoints Class
        /// 3. Complete gestures based on the return value
        private void handleGesture(double x, double y, int id)
        {
            // Push to the movement queue and check time 
            //int hpIndex = currGestures.Count;
            double timeStamp = DateTime.Now.Subtract(typingStartTime).TotalMilliseconds;
            //double timeStamp = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            HandPoint movePoint = new HandPoint(x, y, timeStamp, taskIndex + "_" + hpIndex + "_" + id, HPType.Move);
            handPoints.Add(movePoint);

            GesturePoints myPoints = updateGesturePoints(movePoint, id);
            // If the point exists and status not set
            if (myPoints != null)
            //if (movement.ContainsKey(id))
            {
                // If the status is unknown, try to check its status
                if (GesturePoints.getGestureSwitch() && 
                    (myPoints.getStatus() == HandStatus.Wait || myPoints.getStatus() == HandStatus.Unknown))
                {
                    HandStatus gestureStatus = myPoints.updateGestureStatus();
                    // Check Distance 
                    if (gestureStatus == HandStatus.Backspace)
                    {
                        // Reset the hpNo (added one when touching)
                        --hpNo;

                        // Delete ONE WORD
                        deleteWord();
                    }
                    else if (gestureStatus == HandStatus.Enter)
                    {
                        // Reset the hpNo (added one when touching)
                        --hpNo;

                        // Show the next task 
                        gotoNextText();

                        // TODO: Output 'Enter' if applicable (in real text editor)
                    }
                    // Do not need to do anything when type

                    //Draw
                    if (circleControl)
                    {
                        if (myPoints.getStatus() == HandStatus.Wait)
                            drawCircle(x, y, moveCircleNearBrush, moveCircleSize);
                        else
                            drawCircle(x, y, moveCircleFarBrush, moveCircleSize);
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
            if (currDevice == InputDevice.Hand && e.TouchDevice.GetIsFingerRecognized() && isTypingStart)
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
            if (currDevice == InputDevice.Mouse && isTypingStart)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    Point touchPos = e.GetPosition(this);
                    int mouseTouchId = hpIndex;
                    if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                    {
                        handleGesture(touchPos.X, touchPos.Y, mouseTouchId);
                    }
                    else
                    {
                        calibrateHands(touchPos.X, touchPos.Y, mouseTouchId, HPType.Move);
                    }
                }
            }
        }

        private void releaseGesture(double x, double y, int id)
        {
            if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
            {
                // Push to the movement queue and check time 
                double timeStamp = DateTime.Now.Subtract(typingStartTime).TotalMilliseconds;
                HandPoint releasePoint = new HandPoint(x, y, timeStamp, taskIndex + "_" + hpIndex + "_" + id, HPType.Release);
                handPoints.Add(releasePoint);

                //Draw
                if (circleControl)
                {
                    drawCircle(x, y, releaseCircleBrush, releaseCircleSize);
                }

                GesturePoints myPoints = updateGesturePoints(releasePoint, id);

                // If the point exists and status not set
                if (myPoints != null)
                //if (movement.ContainsKey(id))
                {
                    // If the status is unknown, try to check its status
                    if (GesturePoints.getGestureSwitch() && 
                        (myPoints.getStatus() == HandStatus.Wait || myPoints.getStatus() == HandStatus.Unknown))
                    {
                        HandStatus gestureStatus = myPoints.updateGestureStatus();
                        // Check Distance 
                        if (gestureStatus == HandStatus.Backspace)
                        {
                            // Reset the hpNo (added one when touching)
                            --hpNo;

                            // Delete ONE WORD
                            deleteWord();
                        }
                        else if (gestureStatus == HandStatus.Enter)
                        {
                            // Reset the hpNo (added one when touching)
                            --hpNo;

                            // Show the next task 
                            gotoNextText();

                            // TODO: Output 'Enter' if applicable (in real text editor)
                        }
                        // Do not need to do anything when type
                    }

                    double movingDist = myPoints.calcMovingParam();
                    Console.WriteLine("Move " + movingDist + " when release.");
                    // Try to fix a system error of the Surface 2.0 Platform
                    // When you touch 2 close points, say pA and pB, in a short time, 
                    // they will be recognized as: pA down -> a short move -> pB up.
                    if (myPoints.getStatus() == HandStatus.Wait && movingDist > TYPING_DIST_THRE)
                    {
                        // Recover the missing point
                        int newId = id * 10;
                        Console.WriteLine("Recover missing point:" + x + "," + y + "," + newId);
                        ++hpIndex;
                        saveTouchPoints(x, y, newId);
                        GesturePoints newPoints = findGesturePoints(newId);
                        newPoints.setStatus(HandStatus.Type);
                        newPoints.getStartPoint().setType(HPType.Recover);
                        drawCircle(x, y, recoverCircleBrush, recoverCircleSize);
                    }
                    
                    myPoints.checkTyping();
                    if (myPoints.getStatus() == HandStatus.Unknown)
                    {
                        --hpNo;
                        updateStatusBlock();
                        updateTaskTextBlk();
                    }
                }
                else
                {
                    Debug.WriteLine("[Error] releaseGesture(): id not exist." + id);
                }
            }
            else
            {
                // If the user raise his finger, then reset. 
                // Note: If the user raise the mouse, don't do this. Just for testing. 
                if (currDevice == InputDevice.Hand)
                {
                    // Reset calibration points 
                    calibStatus = CalibStatus.Preparing;
                    calibPoints.Clear();
                    updateStatusBlock();
                }
            }
        }

        private void InputCanvas_TouchUp(object sender, TouchEventArgs e)
        {
            if (currDevice == InputDevice.Hand && e.TouchDevice.GetIsFingerRecognized() && isTypingStart)
            {
                // Call handleGesture is important if the user just 'touch down' - 'touch up' without movement
                Point touchPos = e.TouchDevice.GetPosition(this);
                if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                {
                    handleGesture(touchPos.X, touchPos.Y, e.TouchDevice.Id);
                }

                releaseGesture(touchPos.X, touchPos.Y, e.TouchDevice.Id);
            }
        }

        private void InputCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (currDevice == InputDevice.Mouse && isTypingStart)
            {
                Point touchPos = e.GetPosition(this);
                int mouseTouchId = hpIndex;
                if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
                {
                    handleGesture(touchPos.X, touchPos.Y, mouseTouchId);
                }

                releaseGesture(touchPos.X, touchPos.Y, mouseTouchId);
            }
        }

        /** Listening to the buttons **/

        private string getTestingTag()
        {
            // Return the tag string for testing status( with/without keyboard .etc) 
            string myTag = "";

            // User ID 
            myTag += "_" + userId;

            switch (currDevice)
            {
                case InputDevice.PhyKbd:
                    myTag += "_PhyKbd";
                    break;

                case InputDevice.Hand:
                case InputDevice.Mouse:
                    if (kbdImgStatus == KbdImgStatus.Off)
                        myTag += "_KbdOff";
                    else
                        myTag += "_KbdOn";

                    if (calibStatus == CalibStatus.Off)
                        myTag += "_CalibOff";
                    else
                        myTag += "_CalibOn";
                    break;
            }

            return myTag;
        }

        /// <summary>
        /// Clear focus on the buttons after clicking. 
        /// </summary>
        /// If not doing so, the Space of physical keyboard will triger the click on the focused button. 
        /// Reference: decasteljau http://stackoverflow.com/a/2914599/4762924 
        private void clearKbdFocus(Button btn)
        {
            FrameworkElement parent = (FrameworkElement)btn.Parent;
            while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
            {
                parent = (FrameworkElement)parent.Parent;
            }

            DependencyObject scope = FocusManager.GetFocusScope(btn);
            FocusManager.SetFocusedElement(scope, parent as IInputElement);
        }

        /// <summary>
        /// Save the touchdown seq to file. '_major' file: major data  
        /// </summary>
        private void SaveBtn_TouchDown(object sender, TouchEventArgs e)
        {
            string fPath = Directory.GetCurrentDirectory() + '\\';
            string fTime = String.Format("{0:MM-dd_HH_mm_ss}", DateTime.Now);
            string fTag = getTestingTag();

            string fTextName = fTime + fTag + "_TaskText.txt";
            string fNameAll = fTime + fTag + ".csv";
            string fNameMajor = fTime + fTag + "_major.csv";

            StatusTextBlk.Text = fPath + fNameAll;

            // Save shuffled text to file 
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fTextName, true))
            {
                foreach (string text in taskTexts)
                {
                    file.WriteLine(text);
                }
            }

            // Save typing data 
            switch (currDevice)
            {
                case InputDevice.PhyKbd:
                    if (currTyping.Length > 0)
                        phyStrings.Add(currTyping + "," + typingTime + "," + deleteNum);

                    // Save raw input strings into file 
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fNameAll, true))
                    {
                        file.WriteLine("RawInput,TypingTime,DeleteNumber");
                        foreach (string str in phyStrings)
                        {
                            file.WriteLine(str);
                        }
                    }

                    clearKbdFocus(SaveBtn);
                    break;

                case InputDevice.Hand:
                case InputDevice.Mouse:
                    // Save raw input points into file 
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fNameAll, true))
                    {
                        file.WriteLine("X, Y, Time, TaskIndex_PointIndex_FingerId, PointType");
                        foreach (HandPoint point in handPoints)
                        {
                            file.WriteLine(point.ToString());
                        }
                    }

                    // Save major data points into '_typing' file 
                    if (currGestures.Count > 0)
                    {
                        foreach (GesturePoints gp in currGestures)
                        {
                            if (gp.getStatus() == HandStatus.Type || gp.getStatus() == HandStatus.Backspace)
                            {
                                validPoints.Add(gp.getStartPoint());
                            }
                        }
                    }

                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fNameMajor, true))
                    {
                        file.WriteLine("X, Y, Time, TaskIndex_PointIndex_FingerId, PointType");
                        foreach (HandPoint point in validPoints)
                        {
                            file.WriteLine(point.ToString());
                        }
                    }
                    break;
            }

            // Clear the timer and storage 
            handPoints.Clear();
            validPoints.Clear();
            currGestures.Clear();
            currTyping = "";
            isTypingStart = false;
            phyStrings.Clear();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
            {
                SaveBtn_TouchDown(null, null);
            }
        }

        /// <summary>
        /// Call when press the next button or do the 'next' gesture or input 'Enter' 
        /// </summary>
        private void gotoNextText()
        {
            taskIndex++;
            hpNo = 0;
            hpIndex = -1;
            isTypingStart = false;

            if (circleControl)
            {
                clearCircles();
            }

            switch (currDevice)
            {
                case InputDevice.PhyKbd:
                    phyStrings.Add(currTyping + "," + typingTime + "," + deleteNum);
                    currTyping = "";
                    isTypingStart = false;
                    break;

                case InputDevice.Hand:
                case InputDevice.Mouse:
                    if (currGestures.Count > 0)
                    {
                        foreach (GesturePoints gp in currGestures)
                        {
                            if (gp.getStatus() == HandStatus.Type || gp.getStatus() == HandStatus.Backspace)
                            {
                                validPoints.Add(gp.getStartPoint());
                            }
                            Console.WriteLine(gp.getStatus());
                        }
                    }
                    //validPoints.AddRange(currValidPoints);
                    currGestures.Clear();
                    //currValidPoints.Clear();
                    calibPoints.Clear();

                    // Clear the status if the calibration mode is ON 
                    if (calibStatus != CalibStatus.Off)
                    {
                        calibStatus = CalibStatus.Preparing;
                    }
                    break;
            }

            deleteNum = 0;
            updateStatusBlock();
            updateTaskTextBlk();

            if (taskIndex == taskTexts.Length)
            {
                MessageBox.Show("Congratulations! You have finished the [" + currDevice + "] test.");
            }
        }

        private void NextBtn_TouchDown(object sender, TouchEventArgs e)
        {
            gotoNextText();
            clearKbdFocus(NextBtn);
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                gotoNextText();

            clearKbdFocus(NextBtn);
        }

        /// <summary>
        ///  Clear the touch points of this sentence£¬and delete the calibration points in the list.
        /// </summary>
        private void clearSentence()
        {
            currGestures.Clear();
            //currValidPoints.Clear();
            calibPoints.Clear();
            hpNo = 0;

            if (calibStatus != CalibStatus.Off)
            {
                calibStatus = CalibStatus.Preparing;
            }

            currTyping = "";
            isTypingStart = false;

            updateStatusBlock();
            updateTaskTextBlk();
        }

        private void ClearBtn_TouchDown(object sender, TouchEventArgs e)
        {
            clearSentence();
            clearKbdFocus(ClearBtn);
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                clearSentence();
            
            clearKbdFocus(ClearBtn);
        }

        /// <summary>
        /// Switch Keyboard Image On/Off
        /// </summary>
        private void switchKbdImg()
        {
            BitmapImage kbdImg;
            switch (kbdImgStatus)
            {
                case KbdImgStatus.SurfSize:
                    kbdImgStatus = KbdImgStatus.PhySize;
                    KeyboardBtn.Background = kbdBtnImgOn;
                    KeyboardBtn.Content = "PhyKbd";

                    kbdImg = kbdImages[(int)KbdImgStatus.PhySize];
                    imgKeyboard.Source = kbdImg;
                    kbdWidth = kbdImg.PixelWidth;
                    kbdHeight = kbdImg.PixelHeight;
                    imgKeyboard.Visibility = Visibility.Visible;
                    break;
                case KbdImgStatus.PhySize:
                    kbdImgStatus = KbdImgStatus.Off;
                    KeyboardBtn.Background = kbdBtnImgOff;
                    KeyboardBtn.Content = "";

                    imgKeyboard.Visibility = Visibility.Hidden;
                    break;

                case KbdImgStatus.Off:
                    kbdImgStatus = KbdImgStatus.SurfSize;
                    KeyboardBtn.Background = kbdBtnImgOn;
                    KeyboardBtn.Content = "SurfKbd";

                    kbdImg = kbdImages[(int)KbdImgStatus.SurfSize];
                    imgKeyboard.Source = kbdImg;
                    kbdWidth = kbdImg.PixelWidth;
                    kbdHeight = kbdImg.PixelHeight;
                    imgKeyboard.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void KeyboardBtn_TouchDown(object sender, TouchEventArgs e)
        {
            switchKbdImg();
            clearKbdFocus(KeyboardBtn);
        }

        private void KeyboardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                switchKbdImg();
                
            clearKbdFocus(KeyboardBtn);
        }

        /// <summary>
        /// Set calibration options 
        /// </summary>
        private void switchCalibOption()
        {
            if (calibStatus == CalibStatus.Off)
            {
                calibPoints.Clear();
                calibStatus = CalibStatus.Preparing;
                CalibBtn.Background = calibBtnImgOn;
            }
            else
            {
                calibStatus = CalibStatus.Off;
                CalibBtn.Background = calibBtnImgOff;
            }
            updateStatusBlock();
            updateTaskTextBlk();
        }

        private void CalibBtn_TouchDown(object sender, TouchEventArgs e)
        {
            switchCalibOption();
            clearKbdFocus(CalibBtn);
        }

        private void CalibBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (currDevice)
            {
                case InputDevice.Mouse:
                    switchCalibOption();
                    break;

                case InputDevice.PhyKbd:
                    // Do not respond if using Physical Keyboard 
                    MessageBox.Show("You don't need to calibrate in Physical Keyboard Mode");
                    break;
            }
            clearKbdFocus(CalibBtn);
        }

        /// <summary>
        /// Delete one word. Make hpNo point to the position of next input char. 
        /// </summary>
        private void deleteWord()
        {
            string currText = taskTexts[taskIndex % taskSize];
            int removeStart = Math.Min(currText.Length, hpNo) - 1;

            // Delete at least one character 
            if (removeStart >= 0)
            {
                for (; removeStart > 0; --removeStart)
                {
                    if (currText[removeStart - 1] == ' ')
                        break;
                }

                switch (currDevice)
                {
                    case InputDevice.PhyKbd:
                        currTyping = currTyping.Substring(0, removeStart);
                        break;

                    case InputDevice.Hand:
                    case InputDevice.Mouse:
                        int validNum = 0;
                        foreach (GesturePoints gp in currGestures)
                        {
                            if (gp.getStatus() == HandStatus.Type)
                            {
                                ++validNum;
                                if (validNum > removeStart)
                                {
                                    gp.setStatus(HandStatus.Backspace);
                                }
                            }
                        }
                        //currValidPoints.RemoveRange(removeStart, hpNo - removeStart);
                        break;
                }

                hpNo = removeStart;
                ++deleteNum;
            }

            updateStatusBlock();
            updateTaskTextBlk();
        }

        private void DeleteBtn_TouchDown(object sender, TouchEventArgs e)
        {
            deleteWord();
            clearKbdFocus(DeleteBtn);
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                deleteWord();

            clearKbdFocus(DeleteBtn);
        }

        /// <summary>
        /// Delete one character. Make hpNo point to the position of next input char. 
        /// </summary>
        private void deleteCharacter()
        {
            string currText = taskTexts[taskIndex % taskSize];
            int removeStart = Math.Min(currText.Length, hpNo) - 1;

            // Delete at least one character 
            if (removeStart >= 0)
            {
                switch (currDevice)
                {
                    case InputDevice.PhyKbd:
                        currTyping = currTyping.Substring(0, removeStart);
                        break;

                    case InputDevice.Hand:
                    case InputDevice.Mouse:
                        int validNum = 0;
                        foreach (GesturePoints gp in currGestures)
                        {
                            if (gp.getStatus() == HandStatus.Type)
                            {
                                ++validNum;
                                if (validNum > removeStart)
                                {
                                    gp.setStatus(HandStatus.Backspace);
                                    //Console.WriteLine("Delete.");
                                }
                            }
                        }
                        //currValidPoints.RemoveRange(removeStart, hpNo - removeStart);
                        break;
                }

                hpNo = removeStart;
                ++deleteNum;
            }

            updateStatusBlock();
            updateTaskTextBlk();
        }

        private void BackspaceBtn_TouchDown(object sender, TouchEventArgs e)
        {
            deleteCharacter();
            clearKbdFocus(BackspaceBtn);
        }

        private void BackspaceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                deleteCharacter();
            clearKbdFocus(BackspaceBtn);
        }

        private void switchInputDevice()
        {
            // Cycle of all input devices defined in the enum InputDevice 
            currDevice = (InputDevice)(((int)currDevice + 1) % Enum.GetNames(typeof(InputDevice)).Length);
            SwitchBtn.Content = currDevice;
            updateWindowTitle();
        }

        private void SwitchBtn_TouchDown(object sender, TouchEventArgs e)
        {
            switchInputDevice();
            clearKbdFocus(SwitchBtn);
        }

        private void SwitchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                switchInputDevice();

            clearKbdFocus(SwitchBtn);
        }

        private void reverseGestureSwitch()
        {
            if (GesturePoints.getGestureSwitch())
                GestureCtrlBtn.Content = "Gesture OFF";
            else
                GestureCtrlBtn.Content = "Gesture ON";
            GesturePoints.reverseGestureSwitch();
        }

        private void GestureCtrlBtn_TouchDown(object sender, TouchEventArgs e)
        {
            reverseGestureSwitch();
            clearKbdFocus(GestureCtrlBtn);
        }

        private void GestureCtrlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                reverseGestureSwitch();

            clearKbdFocus(GestureCtrlBtn);
        }


    }
}