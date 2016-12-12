//
// Eric Butt and Clarisse Vamos
// EEL 4660 - Robotic Systems Final Project
// Robot! At the Disco!
//
// With much regards to Anoop Madhusudanan and his wonderful
// depiction of the Xbox Kinect sensor data and for providing
// the overall structure of our source code
// http://www.amazedsaint.com/2013/10/cakerobot-gesture-driven-robot-that.html
//

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

using Microsoft.Kinect;             //  Find this in 'DLL Files'
using System.IO.Ports;              //  For Serial Control

//  NEED TO SOMEHOW CHANGE THIS!
namespace DiscoRobot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //	Constant variables
        private const float RenderWidth = 640.0f;			//	Width of output drawing
        private const float RenderHeight = 480.0f;			//	Height of our output drawing
        private const double JointThickness = 10;			//	Thickness of drawn joint lines
        private const double BodyCenterThickness = 20;		//	Thickness of body center ellipse
        private const double ClipBoundsThickness = 10;      //	Thickness of clip edge rectangles

        //	Brush variables
        private readonly Brush _centerPointBrush = Brushes.Blue;		  // For skeleton center
        private readonly Brush _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        // For tracked joints
        private readonly Brush _inferredJointBrush = Brushes.Yellow;	  // For inferred joints
        private readonly Pen _trackedBonePen = new Pen(Brushes.Aqua, 6);  // Pen for tracked bones
        private readonly Pen _inferredBonePen = new Pen(Brushes.Gray, 1); // Pen for inferred bones	

        //	Other variables
        private KinectSensor _sensor;						//	The kinect sensor
        private DrawingGroup _drawingGroup;					//	The drawing group for rendering output
        private DrawingImage _imageSource;					//	Drawing image that we will display
        private SerialPort serialPort = new SerialPort();	//	Serial Port for Bluetooth
        private DateTime _prevTime;							//	Current Date&Time


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            serialPort.BaudRate = 9600;
            serialPort.PortName = "COM5"; // Set in Windows
            serialPort.Open();
            _prevTime = DateTime.Now;
            InitializeComponent();

        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this._drawingGroup = new DrawingGroup();					//	Create the drawing group
            this._imageSource = new DrawingImage(this._drawingGroup);	//	Create the image source for image control
            Image.Source = this._imageSource;							//	Display the drawing using the image control

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this._sensor = potentialSensor;
                    break;
                }
            }

            //	If the sensor is active..
            if (null != this._sensor)
            {
                //	Turn on the skeleton stream and add an event handler to update itself
                this._sensor.SkeletonStream.Enable();
                this._sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                //	Try to start the sensor!
                try
                {
                    this._sensor.Start();
                }
                catch (IOException)
                {
                    this._sensor = null;
                }
            }

            //	If the sensor is not active...
            else if (null == this._sensor)
            {
                //	Indicate the Kinect connectionis not active through status bar at bottom of screen
                this.statusBarText.Text = "No ready Kinect found!";
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //	Close the sensor
            if (null != this._sensor)
            {
                this._sensor.Stop();
            }

            //	Close the serial port
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //serialPort.WriteLine("Standby ");
            Skeleton[] skeletons = new Skeleton[0];

            //	Get the skeleton Data
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            //	Draw the Skeleton and Process which commands to send through
            //	the Serial Port and onto the Arduinio.
            using (DrawingContext dc = this._drawingGroup.Open())
            {

                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skeleton in skeletons)
                    {

                        RenderClippedEdges(skeleton, dc);
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            //  THIS IS WHERE YOU DETERMINE WHAT COMMAND TO SEND TO THE ARDUINO

                            //	If enough time has elapsed,
                            DateTime current_time = DateTime.Now;

                            if (current_time.Subtract(_prevTime).TotalMilliseconds >= 250)
                            {
                                //reset timer
                                _prevTime = current_time;

                                //	Get the Joints to decide which serial command to send
                                Joint head = skeleton.Joints[JointType.Head];
                                Joint handRight = skeleton.Joints[JointType.HandRight];
                                Joint handLeft = skeleton.Joints[JointType.HandLeft];
                                Joint elbowRight = skeleton.Joints[JointType.ElbowRight];
                                Joint elbowLeft = skeleton.Joints[JointType.ElbowLeft];
                                Joint shoulderRight = skeleton.Joints[JointType.ShoulderRight];
                                Joint shoulderLeft = skeleton.Joints[JointType.ShoulderLeft];
                                Joint hipLeft = skeleton.Joints[JointType.HipLeft];
                                Joint hipRight = skeleton.Joints[JointType.HipRight];
                                Joint kneeLeft = skeleton.Joints[JointType.KneeLeft];

                                //	ZIG-ZAG Command
                                if (handRight.Position.Y > head.Position.Y &&
                                    handLeft.Position.Y > head.Position.Y)
                                {
                                    serialPort.WriteLine("Z");
                                }
                                //	CIRCLE RIGHT Command
                                else if (handRight.Position.X < elbowRight.Position.X &&
                                         handRight.Position.Y > head.Position.Y)
                                {
                                    serialPort.WriteLine("C");
                                }
                                //	CIRCLE LEFT COMMAND
                                else if (handLeft.Position.X > elbowLeft.Position.X &&
                                         handLeft.Position.Y > head.Position.Y)
                                {
                                    serialPort.WriteLine("D");
                                }
                                //	SPECIAL CASE If both hand are by body, don't turn
                                else if (handRight.Position.X < shoulderRight.Position.X &&
                                         handRight.Position.Y < shoulderRight.Position.Y &&
                                         handLeft.Position.X > shoulderLeft.Position.X &&
                                         handLeft.Position.Y < shoulderLeft.Position.Y)
                                {
                                    serialPort.WriteLine("X");
                                }
                                else if (handRight.Position.X < head.Position.X &&
                                         handRight.Position.Y < head.Position.Y &&
                                         handRight.Position.Y > shoulderLeft.Position.Y)
                                {
                                    serialPort.WriteLine("I");
                                }
                                //	TURN RIGHT Command
                                else if (handRight.Position.X < shoulderRight.Position.X &&
                                         handRight.Position.Y < shoulderRight.Position.Y)
                                {
                                    serialPort.WriteLine("R");
                                }

                                //	TURN LEFT Command
                                else if (handLeft.Position.X > shoulderLeft.Position.X &&
                                         handLeft.Position.Y < shoulderLeft.Position.Y)
                                {
                                    serialPort.WriteLine("L");
                                }

                                //  FORWARD Command
                                else if (handRight.Position.Y > elbowLeft.Position.Y)
                                {
                                    serialPort.WriteLine("F");
                                }
                                // BACKWARD Command
                                else if (handLeft.Position.Y > elbowRight.Position.Y)
                                {
                                    serialPort.WriteLine("B");
                                }
                                //  STOP Command
                                else
                                {
                                    serialPort.WriteLine("X");
                                }

                            }
                            //	Draw the bones and joints
                            this.DrawBonesAndJoints(skeleton, dc);
                        }

                        //	Draw the central skeleton
                        else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this._centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skeleton.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }
                // prevent drawing outside of our render area
                this._drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render all of the Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this._trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this._inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this._sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {

            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this._inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this._trackedBonePen;
            }

            //	Draws the actual Bone here
            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this._sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this._sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this._sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }
    }
}