using System;
using System.Globalization;
using System.IO;

using Microsoft.Kinect;

namespace KinectTest01 {

    /// <summary>
    /// Hauptprogramm.
    /// </summary>
    class Program {

        /// <summary>
        /// Hauptmethode.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args) {

            Console.SetWindowSize(200, 69);

            Console.WriteLine("Hallo Welt!");
           
            KinectSignal ks1 = new KinectSignal();     
            ks1.readSignal();

            while (Console.ReadLine().CompareTo("exit") != 0) {
                Console.WriteLine("Gebe exit ein, um den Sensor auszuschalten.");

            }
           
        }

    }

}
