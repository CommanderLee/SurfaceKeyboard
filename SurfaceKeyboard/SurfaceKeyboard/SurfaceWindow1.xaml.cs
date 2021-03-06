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

        bool                circleControl = false;
        // Circle bias
        const int           circleBiasY = 55;
        
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

        bool                testControl = false;
        bool                isSpacebar = false;
        
        WordPredictor       wordPredictor;
        PredictionMode      predictMode;

        string              currWord, currSentence;
        string              selectSeq;

        // currSelect: highlight current selection. from [0, MAX)
        int                 currSelect;
        SolidColorBrush     selectColor = Brushes.DarkBlue;
        SolidColorBrush     otherColor = Brushes.DarkGreen;
        TextBlock[]         textHints;
        const int           MAX_HINT_NUMBER = 5;

        //const int           SPACE_TOP = 690;
        //const int           SPACE_LEFT = 760;
        //const int           SPACE_RIGHT = 1110;

        //const int           TYPE_LEFT = 703;
        //const int           TYPE_RIGHT = 1284;
        //const int           TYPE_TOP = 507;

        //Point               BACKSPACE_TOP_LEFT = new Point(1214, 507);
        //Point               BACKSPACE_BOTTOM_RIGHT = new Point(1284, 553);

        //Point               DEL_TOP_LEFT = new Point(1234, 553);
        //Point               DEL_BOTTOM_RIGHT = new Point(1284, 597);

        //Point               ENTER_TOP_LEFT = new Point(1173, 597);
        //Point               ENTER_BOTTOM_RIGHT = new Point(1281, 642);

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
            kbdImages[statusId].UriSource = new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_physical_shape.png");
            kbdImages[statusId].EndInit();

            // Physical Keyboard Image 
            statusId = (int)KbdImgStatus.PhySize;
            kbdImages[statusId] = new BitmapImage();
            kbdImages[statusId].BeginInit();
            kbdImages[statusId].UriSource = new Uri(BaseUriHelper.GetBaseUri(this), "Resources/keyboard_physical.png");
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

            wordPredictor = new WordPredictor();
            predictMode = PredictionMode.AbsoluteMode;
            currWord = "";
            currSentence = "";
            selectSeq = "";
            textHints = new TextBlock[] { TextHintBlk0, TextHintBlk1, TextHintBlk2, TextHintBlk3, TextHintBlk4 };
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
            string asteriskText = rgx.Replace(currText, "*");

            if (calibStatus == CalibStatus.Off || calibStatus == CalibStatus.Done)
            {
                if (testControl)
                {
                    // Show prediction sentence
                    string typeText = currSentence + currWord;
                    if (typeText.Length > currText.Length)
                        typeText = typeText.Substring(0, currText.Length);
                    TaskTextBlk.Text = currText + "\n" + typeText + "|";
                }
                else
                {
                    // Show asterisk feedback
                    if (hpNo >= 0 && hpNo <= asteriskText.Length)
                        asteriskText = asteriskText.Substring(0, hpNo) + "|";
                    TaskTextBlk.Text = currText + "\n" + asteriskText;
                }
            }
            else
            {
                // Use asterisk while calibrating 
                TaskTextBlk.Text = asteriskText;
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

                            StatusTextBlk.Text = String.Format("Task:{0}/{1}\n({2}) X:{3}, Y:{4}, Time:{5}, ID:{6}\nhpNo:{7}",
                                taskIndex + 1, taskSize, hpIndex, hpLast.getX(), hpLast.getY(), hpLast.getTime(), hpLast.getId(), hpNo);
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
                int movementLevel = findPoints.Add(touchPoint);
                if (testControl && movementLevel != 0)
                {
                    findPoints.setStatus(HandStatus.Select); 
                    updateSelection(movementLevel);
                    //--hpNo;

                    // Give feedback when moving fingers
                    updateHint();
                }
            }
            else 
            {
                double x = touchPoint.getX();
                double y = touchPoint.getY();

                if (!(testControl && predictMode == PredictionMode.DirectMode && (
                                wordPredictor.isBackspace(x, y) || wordPredictor.isDel(x, y) || wordPredictor.isEnter(x, y))))
                {
                    GesturePoints myPoints = new GesturePoints(touchPoint, HandStatus.Wait);
                    currGestures.Add(myPoints);
                }
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

        //private bool checkValidArea(double x, double y)
        //{
        //    bool isValid = true;
        //    if (x < TYPE_LEFT || x > TYPE_RIGHT || y < TYPE_TOP)
        //        isValid = false;
        //    return isValid;
        //}

        private bool checkSpecialBtn(double x, double y)
        {
            bool isSpecial = false;
            //if ( (x >= BACKSPACE_TOP_LEFT.X && x <= BACKSPACE_BOTTOM_RIGHT.X && y >= BACKSPACE_TOP_LEFT.Y && y <= BACKSPACE_BOTTOM_RIGHT.Y) ||
            //    (x >= DEL_TOP_LEFT.X && x <= DEL_BOTTOM_RIGHT.X && y >= DEL_TOP_LEFT.Y && y <= DEL_BOTTOM_RIGHT.Y) )
            if (wordPredictor.isBackspace(x, y) || wordPredictor.isDel(x, y))
            {
                // Backspace & Del
                deleteCharacter();
                isSpecial = true;
            }
            //else if (x >= ENTER_TOP_LEFT.X && x <= ENTER_BOTTOM_RIGHT.X && y >= ENTER_TOP_LEFT.Y && y <= ENTER_BOTTOM_RIGHT.Y)
            else if (wordPredictor.isEnter(x, y))
            {
                // Enter
                gotoNextText();
                isSpecial = true;
            }
            else
            {
                isSpecial = false;
            }
            //Console.WriteLine("Special: " + isSpecial);
            return isSpecial;
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

            if (testControl && predictMode == PredictionMode.DirectMode && checkSpecialBtn(x, y))
            {
                --hpIndex;
                return;
            }

            Console.WriteLine("~~~~~DoubleCheck:" + wordPredictor.isDel(x, y) + wordPredictor.isBackspace(x, y));

            // Add new point(should return null because it is new)
            if (updateGesturePoints(touchPoint, id) == null)
            {
                // Real-time UI feedback. Note: Change its status later after released.
                ++hpNo;

                if (checkSpace(x, y))
                {
                    // Disable this space point
                    GesturePoints thisPoints = findGesturePoints(id);
                    thisPoints.setStatus(HandStatus.Spacebar);
                    Console.WriteLine("Set status to Spacebar.");
                }
                else
                {
                    updateHint();
                }
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
                    if (myPoints.getStatus() == HandStatus.Select)
                    {
                        // Reset the hpNo (added one when touching)
                        --hpNo;
                    }
                    else
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
                            if (!(testControl && predictMode == PredictionMode.DirectMode && (
                                wordPredictor.isBackspace(x, y) || wordPredictor.isDel(x, y) || wordPredictor.isEnter(x, y) || wordPredictor.isSpacebar(x, y))))
                            {
                                GesturePoints newPoints = findGesturePoints(newId);
                                newPoints.setStatus(HandStatus.Type);
                                newPoints.getStartPoint().setType(HPType.Recover);
                                if (circleControl)
                                {
                                    drawCircle(x, y, recoverCircleBrush, recoverCircleSize);
                                }
                            }
                        }

                        myPoints.checkTyping();
                        if (myPoints.getStatus() == HandStatus.Unknown)
                        {
                            --hpNo;
                            updateStatusBlock();
                            updateTaskTextBlk();
                        }
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

                    //if (calibStatus == CalibStatus.Off)
                    //    myTag += "_CalibOff";
                    //else
                    //    myTag += "_CalibOn";
                    if (testControl)
                        myTag += "_" + predictMode;
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
            string fNameTest = fTime + fTag + "_test.csv";

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
                    if (testControl)
                    {
                        // Save another file, similar to the Physical Keyboard Test
                        if (currWord.Length > 0)
                        {
                            currSentence += currWord;
                            phyStrings.Add(currSentence + "," + handPoints.Last().getTime() + "," + deleteNum + "," + selectSeq);
                        }

                        // Save raw input strings into file 
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(fPath + fNameTest, true))
                        {
                            file.WriteLine("RawInput,TypingTime,DeleteNumber, SelectSequence");
                            foreach (string str in phyStrings)
                            {
                                file.WriteLine(str);
                            }
                        }
                    }

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
                            if (gp.getStatus() == HandStatus.Type || gp.getStatus() == HandStatus.Backspace || gp.getStatus() == HandStatus.Spacebar)
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
            string currText = taskTexts[taskIndex % taskSize];
            if (hpNo >= currText.Length)
            {
                taskIndex++;
                hpNo = 0;
                hpIndex = -1;
                isTypingStart = false;
                isSpacebar = false;

                clearCircles();

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
                                if (gp.getStatus() == HandStatus.Type || gp.getStatus() == HandStatus.Backspace || gp.getStatus() == HandStatus.Spacebar)
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

                        if (testControl)
                        {
                            currSentence += currWord;
                            resetSelection();
                            updateHint();
                            // Save to file/string
                            phyStrings.Add(currSentence + "," + handPoints.Last().getTime() + "," + deleteNum + "," + selectSeq);

                            Console.WriteLine("Save: " + currSentence);
                        }
                        currSentence = "";
                        currWord = "";
                        selectSeq = "";

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
                else if (taskIndex % 10 == 0)
                {
                    MessageBox.Show("You have finished Session #" + (taskIndex / 10) + ".");
                }
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
        ///  Clear the touch points of this sentence��and delete the calibration points in the list.
        /// </summary>
        private void clearSentence()
        {
            currGestures.Clear();
            //currValidPoints.Clear();
            calibPoints.Clear();
            hpNo = 0;
            deleteNum = 0;

            if (calibStatus != CalibStatus.Off)
            {
                calibStatus = CalibStatus.Preparing;
            }

            currWord = "";
            currSentence = "";
            selectSeq = "";
            resetSelection();

            currTyping = "";
            isTypingStart = false;
            isSpacebar = false;
            clearCircles();
            updateHint();
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

            isSpacebar = false;

            // Delete at least one character 
            if (removeStart >= 0)
            {
                if (testControl)
                {
                    if (removeStart >= currSentence.Length)
                        removeStart = currSentence.Length;
                    else
                    {
                        for (; removeStart > 0; --removeStart)
                        {
                            if (currSentence[removeStart - 1] == ' ')
                                break;
                        }
                    }
                }
                else
                {
                    for (; removeStart > 0; --removeStart)
                    {
                        if (currText[removeStart - 1] == ' ')
                            break;
                    }
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

                if (testControl)
                {
                    currWord = "";
                    // Delete word and affect currSentence. If not, only affect currWord.
                    if (hpNo < currSentence.Length)
                    {
                        currSentence = currSentence.Substring(0, hpNo);
                    }
                    updateHint();
                }
                updateStatusBlock();
                updateTaskTextBlk();
            }
            else
            {
                clearSentence();
            }
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
            //Console.WriteLine("Delete: hpNo:" + hpNo);
            string currText = taskTexts[taskIndex % taskSize];
            int removeStart = Math.Min(currText.Length, hpNo) - 1;
            isSpacebar = false;
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
                            if (gp.getStatus() == HandStatus.Type || gp.getStatus() == HandStatus.Spacebar)
                            {
                                ++validNum;
                                if (validNum > removeStart)
                                {
                                    gp.setStatus(HandStatus.Backspace);
                                    //Console.WriteLine("----Delete. validNum:" + validNum);
                                }
                            }
                        }
                        //Console.WriteLine("----Valid Num:" + validNum);
                        break;
                }

                hpNo = removeStart;
                ++deleteNum;

                if (testControl)
                {
                    // Doesnot affect currSentence if hpNo >= currSentenceLen. 
                    if (hpNo < currSentence.Length)
                    {
                        // Affect a char of currSentence
                        for (; removeStart > 0; --removeStart)
                        {
                            if (currSentence[removeStart - 1] == ' ')
                                break;
                        }
                        currSentence = currSentence.Substring(0, removeStart);
                    }
                    currWord = "";
                    updateHint();
                }
                updateStatusBlock();
                updateTaskTextBlk();
            }
            else
            {
                clearSentence();
            }
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

        private void switchCircle()
        {
            if (circleControl)
            {
                CircleBtn.Content = "Circle OFF";
            }
            else
            {
                CircleBtn.Content = "Circle ON";
            }
            circleControl = !circleControl;
        }

        private void CircleBtn_TouchDown(object sender, TouchEventArgs e)
        {
            switchCircle();
            clearKbdFocus(CircleBtn);
        }

        private void CircleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                switchCircle();

            clearKbdFocus(CircleBtn);
        }

        private void resetSelection(int target = 0)
        {
            textHints[currSelect].Background = otherColor;
            currSelect = target;
            textHints[currSelect].Background = selectColor;
        }

        private void updateSelection(int movementLevel)
        {
            // Find existing one:
            if (movementLevel > 0)
            {
                // Move to right
                int maxI = Math.Min(MAX_HINT_NUMBER - 1, currSelect + movementLevel);
                for (var i = currSelect; i <= maxI; ++i)
                {
                    if (textHints[i].Text == "")
                    {
                        // Exceed.
                        break;
                    }
                    else
                    {
                        textHints[currSelect].Background = otherColor;
                        currSelect = i;
                        textHints[currSelect].Background = selectColor;
                    }
                }
            }
            else
            {
                // Move to left. Note: movementLevel < 0.
                int minI = Math.Max(0, currSelect + movementLevel);
                resetSelection(minI);
            }
            
            updateHint();
            currWord = textHints[currSelect].Text;
            updateTaskTextBlk();
            //Console.WriteLine("Update: " + movementLevel);
            //Console.WriteLine("Change to: " + currWord + "(" + TaskTextBlk.Text + ")");
        }

        private bool checkSpace(double x, double y)
        {
            bool thisIsSpacebar = false;
            if (wordPredictor.isSpacebar(x, y))
            {
                thisIsSpacebar = true;
                Console.WriteLine("Space.");
            }

            // Process space: last char is space
            if (testControl)
            {   
                // This char is space
                if (thisIsSpacebar)
                {
                    currWord += " ";
                    foreach (TextBlock tb in textHints)
                    {
                        tb.Text += " ";
                    }
                }

                // Last char is space
                if (isSpacebar)
                {
                    currSentence += currWord;
                    currWord = "";
                    resetSelection();
                }
            }
            isSpacebar = thisIsSpacebar;
            return thisIsSpacebar;
        }

        private void updateHint()
        {
            if (testControl)
            {
                // Convert currGesture -> double array X/Y
                int maxListLen = 100;
                int counter = 0;
                List<double> listX, listY;
                listX = new List<double>();
                listY = new List<double>();

                Console.WriteLine("UpdateHint: Search from " + currGestures.Count + " points.");
                Console.WriteLine("    currSentence:" + currSentence + "; currWord:" + currWord);

                // counter: [0...currSentenceLen-1][currSLen...currSLen+MaxListLen]
                foreach (GesturePoints gp in currGestures)
                {
                    if (counter >= maxListLen + currSentence.Length)
                    {
                        break;
                    }
                    else if (gp.getStatus() == HandStatus.Type || gp.getStatus() == HandStatus.Wait || gp.getStatus() == HandStatus.Spacebar)
                    {
                        if (counter >= currSentence.Length && (gp.getStatus() == HandStatus.Type || gp.getStatus() == HandStatus.Wait))
                        {
                            listX.Add(gp.getStartPoint().getX());
                            listY.Add(gp.getStartPoint().getY());
                        }
                        ++counter;
                    }
                }

                //Console.WriteLine("    " + listX.Count + " valid touch points.");

                if (listX.Count > 0)
                {
                    List<KeyValuePair<string, double>> probWords = wordPredictor.predict(listX.ToArray(), listY.ToArray(), predictMode);
                    //Console.WriteLine("    " + probWords + " probable words.");

                    // Show hint in the TextHintBlock
                    counter = 0;

                    Console.WriteLine("Candidates:");
                    for (var i = 0; i < probWords.Count; ++i)
                    {
                        string tempWord = probWords[i].Key;
                        Console.WriteLine("    :" + tempWord + ", " + probWords[i].Value);
                        if (counter >= MAX_HINT_NUMBER)
                        {
                            break;
                        }
                        //else if (tempWord.Contains('\'') || (i > 0 && probWords[i - 1].Key == tempWord))
                        //{
                        //    // ignore same word or strange word(I'm Jack's .etc.)
                        //    continue;
                        //}
                        else
                        {
                            // Update default prediction:
                            if (counter == currSelect)
                            {
                                currWord = tempWord;
                                textHints[counter].Background = selectColor;
                                Console.WriteLine("Default Word: " + currWord);
                            }
                            else
                            {
                                textHints[counter].Background = otherColor;
                            }
                            textHints[counter].Text = tempWord;
                            ++counter;
                        }
                    }
                    for (var c = counter; c < MAX_HINT_NUMBER; ++c)
                    {
                        textHints[c].Text = "";
                    }
                }
                else
                {
                    foreach (TextBlock tb in textHints)
                    {
                        tb.Text = "";
                    }
                }
            }
        }

        private void switchTest()
        {
            if (testControl)
            {
                TestBtn.Content = "Test OFF";
            }
            else
            {
                if (!wordPredictor.loadStatus)
                {
                    wordPredictor.initialize();

                    // Debug:
                    //double[] x = new double[] {1,2,3};
                    //double[] y = new double[] {4,5,6};
                    //List<string> probWords = wordPredictor.predict(x, y);
                }
                TestBtn.Content = "Test ON";
            }
            testControl = !testControl;
        }

        private void TestBtn_TouchDown(object sender, TouchEventArgs e)
        {
            switchTest();
            clearKbdFocus(TestBtn);
        }

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                switchTest();

            clearKbdFocus(TestBtn);
        }

        private void switchPredictMode()
        {
            if (predictMode == PredictionMode.DirectMode)
            {
                PredictBtn.Content = "Relative";
                predictMode = PredictionMode.RelativeMode;
            }
            else if (predictMode == PredictionMode.RelativeMode)
            {
                PredictBtn.Content = "Absolute";
                predictMode = PredictionMode.AbsoluteMode;
            }
            else if (predictMode == PredictionMode.AbsoluteMode)
            {
                PredictBtn.Content = "Direct";
                predictMode = PredictionMode.DirectMode;
            }
            else
            {
                Console.WriteLine("Wrong Mode Info");
            }
        }

        private void PredictBtn_TouchDown(object sender, TouchEventArgs e)
        {
            switchPredictMode();
            clearKbdFocus(PredictBtn);
        }

        private void PredictBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currDevice != InputDevice.Hand)
                switchPredictMode();

            clearKbdFocus(PredictBtn);
        }

        private void clickOnTextHint(int num)
        {
            if (testControl)
            {
                resetSelection(num);
                currWord = textHints[num].Text;
                if (selectSeq == "")
                    selectSeq += currWord + ":" + num.ToString();
                else
                    selectSeq += "-" + currWord + ":" + num.ToString();
                updateTaskTextBlk();
            }
        }

        private void TextHintBlk0_TouchDown(object sender, TouchEventArgs e)
        {
            clickOnTextHint(0);
        }

        private void TextHintBlk0_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clickOnTextHint(0);
        }

        private void TextHintBlk1_TouchDown(object sender, TouchEventArgs e)
        {
            clickOnTextHint(1);
        }

        private void TextHintBlk1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clickOnTextHint(1);
        }

        private void TextHintBlk2_TouchDown(object sender, TouchEventArgs e)
        {
            clickOnTextHint(2);
        }

        private void TextHintBlk2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clickOnTextHint(2);
        }

        private void TextHintBlk3_TouchDown(object sender, TouchEventArgs e)
        {
            clickOnTextHint(3);
        }

        private void TextHintBlk3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clickOnTextHint(3);
        }

        private void TextHintBlk4_TouchDown(object sender, TouchEventArgs e)
        {
            clickOnTextHint(4);
        }

        private void TextHintBlk4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clickOnTextHint(4);
        }

    }
}