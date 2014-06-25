using System;
using System.Globalization;
using System.IO;

using Microsoft.Kinect;

namespace KinectTest01
{

    /// <summary>
    /// Hauptprogramm.
    /// </summary>
    class Program
    {

        /// <summary>
        /// Hauptmethode.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            Console.SetWindowSize(200, 69);

            Console.WriteLine("Hallo Welt!");

            KinectSignal ks1 = new KinectSignal();
            // ks1.TestProgramm();

            ks1.readSignal();


            while (Console.ReadLine().CompareTo("exit") != 0)
            {
                Console.WriteLine("Gebe asdf ein, um den Sensor auszuschalten.");

            }

        }

    }

}
/*
 * 
//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// //Hauptfenster (Hauptanwendung)
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;
        

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermediate storage for the depth data converted to color
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        /// 
        //Konstruktoraufruf oder so? Das wird am Anfang aufgerufen und initialisiert das Fenster, was man dann sieht.
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            /*
             * Das Fenster, dass in InitializeComponent initialisiert wurde, ruft nach dem Laden WindowLoaded auf (siehe MainWindow.xaml, da ist der Funktionsaufruf)
             * 

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }
         
            if (null != sensor)
            {

                // Turn on the depth stream to receive depth frames
                sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

                //Nach dieser Zeile hat sensor.DepthStream auch die Daten über die Größe des Bildes unter FramePixelDataLength gespeichert
                
                // Allocate space to put the depth pixels we'll receive
                depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];

                // Allocate space to put the color pixels we'll create
                colorPixels = new byte[sensor.DepthStream.FramePixelDataLength * sizeof(int)];

                // This is the bitmap we'll display on-screen
                colorBitmap = new WriteableBitmap(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                //PixelFormats.Bgr32 heisst: "32 Bit pro Pixel, codiert nach Blau/Grün/Rot"
               
                //Image gehört zum Fenster?
                // Set the image we display to point to the bitmap where we'll put the image data
                Image.Source = colorBitmap;

                //Addition eines Eventhandlers (a+=b) sieht komisch aus, aber das heisst, dass eine Kausalbeziehung zwischen a und b hergestellt wird;
                //a -> b, d.h. wenn a aufgerufen wird, wird auch b aufgerufen
                //hier heisst es: "Wenn sensor einen depth-frame bereit hat, rufe die Methode SensorDepthFrameReady auf"
                //Somit die wichtigste Stelle in dieser Methode, da hier die Aufrufe der Tiefensensor-Methoden erfolgen
                //Der Methodenaufruf hat hier an dieser Stelle keine Argumente, aber diese werden automatisch übergeben? (s. die Methode SensorDepthFrameReady selbst,
                //die 2 Argumente hat

                // Add an event handler to be called whenever there is new depth frame data
                sensor.DepthFrameReady += SensorDepthFrameReady;
               
                // Start the sensor!
                //So ruft dann der Sensor von selbst Daten und somit die verbundenen Methoden auf
                try
                {
                    sensor.Start();
                }
                catch (IOException)
                {
                    sensor = null;
                }
            }

            if (null == sensor)
            {
                statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }
        
        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != sensor)
            {
                sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            /*
             using (Variablendeklaration) heisst, dass nur für diesen Block die Variable deklariert wird, also nur in diesem Block
             * existiert sie und am Ende des Blocks wird sie -automatisch- freigemacht/entfernt, ohne dass man das von Hand machen muss.
             * 
             * e ist ein Argument dieser Funktion und OpenDepthImageFrame() gibt das Signal zurück, dass automatisch beim
             * Aufruf als Argument übergeben wird, also ist da drin das Signal?
            
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                //wenn der Speicher für das aktuelle Signal nicht leer ist (also vorliegt)
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    //depthPixels hat danach die Pixel-Daten aus dem Signal, das von e bzw. depthFrame kommt
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);

                    // Get the min and max reliable depth for the current frame
                    //die min/max-DepthDaten kommen also aus depthFrame?
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB
                    int colorPixelIndex = 0;
                    for (int i = 0; i < depthPixels.Length; ++i)   //für alle Pixeldaten
                    {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.

                        /*
                         Hier erfolgt die Umwandlung von depth (was ein short-Wert ist) in einen byte-Wert, und zwar so:
                         * Wenn depth zwischen min/max-Depth liegt, bekommt intensity den Wert von depth, ansonsten 0
                         * short hat 16 Bits (2 Byte) Platz, byte nur 8 Bits (1 Byte) Platz, es geht also Information verloren beim Umwandeln.
                         * Most-significant bit ist ein Begriff (siehe Wikipedia "Bitwertigkeit") und bezeichnet die Bits ganz links, die den Wert der repräsentierten
                         * Zahl sehr stark beeinflussen (höchste Wertigkeit besitzen). Diese Bits gehen verloren.
                         
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                        //zur Erinnerung: colorPixels ist vom Typ byte[]
                        /*
                         * Es wird jetzt das Array colorPixels beschrieben mit allen Farbwerten hintereinander, also
                         * B1,G1,R1,B2,G2,R2,B3,..., wobei die Zahl die Nummer des Pixels beschreibt.
                         * 
                         * Das Allokieren von Speicher für colorPixels passiert in WindowLoaded:
                         * colorPixels = new byte[sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                         * FramePixelDataLength ist Anzahl der Pixel und sizeof(int) ist 4 (glaube ich), also so viel Platz, sodass man für jeden
                         * Pixel 4 Werte (BGR,Alpha) speichern kann.
                         *
                        // Write out blue byte
                        colorPixels[colorPixelIndex++] = intensity;

                        // Write out green byte
                        colorPixels[colorPixelIndex++] = intensity;

                        // Write out red byte                        
                        colorPixels[colorPixelIndex++] = intensity;
                                                
                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    // Write the pixel data into our bitmap
                    colorBitmap.WritePixels(
                        new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight), //das zu zeichnende Viereck (Rahmen?)
                        colorPixels,   //die Pixeldaten selbst
                        colorBitmap.PixelWidth * sizeof(int),  //stride, d.h. Abstand vom Anfang einer Pixelreihe zum Ende einer Pixelreihe
                        0); //offset?
                }
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            if (null == sensor)
            {
                statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            /*
             * Die Bilddaten kommen also beim Screenshot aus colorBitmap her. Es müsste also die Daten irgendwie speichern.
             * 
             * 
            encoder.Frames.Add(BitmapFrame.Create(colorBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            }
        }
        
        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (sensor != null)
            {
                // will not function on non-Kinect for Windows devices
                try
                {
                    if (checkBoxNearMode.IsChecked.GetValueOrDefault())
                    {
                        sensor.DepthStream.Range = DepthRange.Near;
                    }
                    else
                    {
                        sensor.DepthStream.Range = DepthRange.Default;
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
 
 */
