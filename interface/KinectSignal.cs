using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System;
using System.Globalization;
using System.IO;

using Microsoft.Kinect;

namespace KinectTest01 {

    ///Klasse, die Signale der Kinect in ein geeignetes Format umgeschreibt.
    class KinectSignal {
        /// <summary>
        /// aktueller Kinect-Sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        DepthImagePixel[] depthPixels;

        //Wuerfelgroesse
        const byte CUBE_SIZE = 4;

        /// <summary>
        /// Minimaltiefe, in der Signal erfasst werden soll. Standard: 500 
        /// </summary>
        int minDepth = 700;

        /// <summary>
        /// Maximaltiefe. Standard: 1000
        /// </summary>
        int maxDepth = 850;

        /// <summary>
        /// Gibt an (zwischen 0 und 1), welcher Anteil des gesamten Bildes (vom Zentrum aus gesehen) verarbeitet werden soll.
        /// </summary>
        double screenRatio = 0.15; //Standard: 0.3

        bool toPrint = false;

        /// <summary>
        /// Gibt an wie lange ein Schleifendurchlauf dauert, bis ein Signal erneut gelesen wird.
        /// Muss genauso groß wie timePerPattern im .ino-Sketch sein
        /// Gute Werte: 60, 90 (bzw. 59/89)
        /// </summary>
        int delayTime =59;
        int counater = 0;

        /// <summary>
        /// Wie groß das Bild ist, das Kinect aufnehmen soll.
        /// </summary>
        short bildBreite = 80;
        short bildHoehe = 60;

        /// <summary>
        /// Der SerialPort, über den die Daten an COM3-Anschluss gesendet werden
        /// </summary>
        SerialPort port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);

        /// <summary>
        /// Standardkonstruktor.
        /// </summary>
        public KinectSignal() {

        }

        ///Haupt-Methode, den Sensor anschaltet und das Empfangen von Signalen ermöglicht.
        public void readSignal() {
            Console.WriteLine("readSignal wurde betreten!");

            foreach (var potentialSensor in KinectSensor.KinectSensors) {
                if (potentialSensor.Status == KinectStatus.Connected) {
                    sensor = potentialSensor;
                    break;
                }
            }


            if (null != sensor) {

                // Aktiviert den Tiefen Stream, nach diesem Aufruf hat die Methode auch die Daten über die Größe des Bildes unter FramePixelDataLength gespeichert
                sensor.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);
                depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];

                //Addition eines Eventhandlers (a+=b) bedeutet, dass eine Kausalbeziehung zwischen a und b hergestellt wird.
                //a -> b, d.h. wenn a aufgerufen wird, wird auch b aufgerufen
                //hier bedeutet es: "Wenn sensor einen depth-frame bereit hat, rufe die Methode VerarbeiteSensorFrameDaten auf"
                sensor.DepthFrameReady += VerarbeiteSensorFrameDaten;

                port.Open();
             
				//Sensor starten. So ruft dann der Sensor von selbst Daten und somit die verbundenen Methoden auf
                try {
                    sensor.Start();
                } catch (IOException e) {
                    Console.WriteLine("IOException! " + e);
                    sensor = null;
                }
            } else {
                Console.WriteLine("sensor ist null");
            }
            Console.WriteLine("ReadSignal verlassen!");
        }

        /// <summary>
        /// Die wichtigste Methode, hier werden die Sensordaten verarbeitet. Sie wird als Callback in Read-Signal aufgerufen.
        /// </summary>
        /// <param name="sender">Objekt, dass Methode aufruft?</param>
        /// <param name="e">Das Signal, wird automatisch beim Callback übergeben.</param>
        private void VerarbeiteSensorFrameDaten(object sender, DepthImageFrameReadyEventArgs e) {
            
			short[,] depthValues2D = new short[bildHoehe, bildHoehe]; //1. Index: Zeile, 2. Index: Spalte
            short[,] depthValues2DSmall;
            short[,] depthValuesFiltered;
            String depthValuesString;
            
			/*
             using (Variablendeklaration) heisst, dass nur für diesen Block die Variable deklariert wird, also nur in diesem Block
             * existiert sie und am Ende des Blocks wird sie -automatisch- freigemacht/entfernt, ohne dass man das von Hand machen muss.
             * 
             * e ist ein Argument dieser Funktion und OpenDepthImageFrame() gibt das Signal in e zurück, dass automatisch beim
             * Aufruf als Argument e übergeben wird
             * 
            */
						
			using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {
                
				if (depthFrame != null) {
                 
                    //Pixeldaten des aktuellen Frames kopieren aus depthFrame in depthPixels
                    //depthPixels hat danach die Pixel-Daten aus dem Signal, das von e bzw. depthFrame kommt
                    //(depthPixels ist ein Array, hat also die Daten von allen Pixeln!)
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);
                    
                    //Das Bild muss quadratisch gemacht werden, hier durch Abschneiden der Ränder, was in der Methode passiert.
                    //Der Würfel muss dabei mit der Rückseite (gegenüber dem Anschluss) zum Betrachter zeigen, damit die Richtungen stimmen.
                    depthValues2D = GetQuadraticDepthValuesArray(depthPixels);
                    
					// Die Variable toPrint befindet sich im Kopf dieser Datei und wurde zu Debugging Zwecke verwendet.
					// Standard : false, also keine Testausgaben
                    if (toPrint) {
                    
                        screenRatio = 0.2;
                        depthValues2DSmall = CropDepthData(depthValues2D, screenRatio);
                        PrintDepthData(depthValues2DSmall);

                        depthValues2DSmall = ShrinkingBySkipping2D(depthValues2DSmall, CUBE_SIZE);
                        Console.WriteLine("Verkleinert:");
                        Console.WriteLine();
                        depthValuesFiltered = FilterAndResizeNumbers(depthValues2DSmall, minDepth, maxDepth);

                        Console.WriteLine("Gefiltert: ");
                        PrintDepthData(depthValuesFiltered);

                        depthValuesFiltered = SwitchColumns(depthValuesFiltered);

                        Console.WriteLine("Vertauscht:");

                        Console.WriteLine();
                        depthValuesString = Convert2DToString(depthValuesFiltered);
                        Console.WriteLine("Finaler String: " + depthValuesString);

                        SendDataString(depthValuesString);
                        Console.Clear();
                    } else {
                        SendDataString(
                           Convert2DToString(
                            
                                    FilterAndResizeNumbers(
                                        ShrinkingBySkipping2D(
                                            CropDepthData(
                                                depthValues2D,
                                                screenRatio
                                            ),
                                            CUBE_SIZE),
                                        minDepth,
                                        maxDepth
                                    )
                          
                            )
                        );
                    }
                    System.Threading.Thread.Sleep(delayTime);
                } else {
                    Console.WriteLine("Es ist kein Signal in VerarbeiteSensorFrameDaten reingekommen!");
                }
            }
        }

        /// <summary>
        /// Gibt die Tiefenwerte als Zahlen aus den DepthImagePixeln (die mehr Information neben den Zahlen beinhalten) als quadratisches Array zurück.
        /// Ränder werden somit abgeschnitten, falls das Kinect-Bild nicht quadratisch ist.
        /// 
        /// Der Aufbau des Arrays ist aber ein bisschen komisch, es läuft irgendwie von oben nach unten und von rechts (!) nach links
        /// Deswegen das Füllen von depthValues2D etwas anders, und zwar läuft die innere Schleife von bildHoehe-1 bis 0 und nicht umgekehrt von
        /// 0 bis bildHoehe-1, sozusagen wird das Bild horizontal verkehrt eingelesen. Es sieht so aber dennoch richtig aus: Der Würfel muss
        /// dabei mit der Rückseite (gegenüber dem Anschluss) zum Betrachter zeigen.
        /// 
        /// Wenn wieder alter Zustand hergestellt werden soll, Grenzen anpassen in der inneren Schleife (müsste als Kommentar da sein, dann die vorhandene
        /// Schleife wieder auskommentieren).
        /// </summary>
        /// <param name="dip">Feld mit DepthImagePixeln</param>
        /// <returns>Quadratisches 2D-Array mit Zahlenwerten</returns>
        short[,] GetQuadraticDepthValuesArray(DepthImagePixel[] dip) {
            short[,] result = new short[bildHoehe,bildHoehe];
            int counter = 0;
            for (int i = 0; i < bildHoehe; i++) {
                counter += (bildBreite - bildHoehe) / 2;
                for (int j = bildHoehe-1; j >=0; j--) {
                    result[i, j] = depthPixels[counter].Depth;
                    counter++;
                }
                counter += (bildBreite - bildHoehe) / 2;
            }
            return result;
        }

        /// <summary>
        /// Schneidet Ränder des (quadratischen!) Arrays so ab, dass ein kleineres Quadrat in der Mitte rauskommt. 
        /// </summary>
        /// <param name="ratio">Wie gross das neue Bild im Vergleich zum alten sein soll. Zu beachten ist, dass Ratio in der Berechnung nur auf die Seitenlängen
        /// des Quadrats einbezogen wird, die Gesamtfläche beträgt nur noch (ratio)^2, da Fläche=Seitenlänge^2 ist (z.B. 0.5 für 50% Seitenlänge, 25% Gesamtfläche).</param>
        /// <returns>Verkleinertes Array</returns>
        short[,] CropDepthData(short[,] data, double ratio) {
            int newSize = (int)((double)data.GetLength(0) * ratio);

            if (ratio > 1) {    //Werte größer als 1 wieder auf 1 zurücksetzen
                newSize = data.GetLength(0);
                ratio = 1;
            }

            int offset = (int)((double)data.GetLength(0) * (double)(1.0 - ratio) / 2.0);
            short[,] result = new short[newSize, newSize];
            //Console.WriteLine("Anteil: " + ratio);

            for (int i = 0; i < newSize; i++) {
                for (int j = 0; j < newSize; j++) {
                    result[i, j] = data[i + offset, j + offset];
                }
            }
            return result;
        }
        /// <summary>
        /// Verkleinert die Tiefendaten auf eine gegebene Groesse. Ausgangspunkt ist das gesamte Bild, was problematisch sein könnte.
        /// </summary>
        /// <param name="data">Zweidimensionales quadratisches(!) Array</param>

        /// <param name="goalSize">Größe des -zweidimensionalen, quadratischen- Arrays nach der Verkleinerung</param>
        /// <returns>Geschrumpftes Array</returns>
        short[,] ShrinkingBySkipping2D(short[,] data, int goalSize) {
            /*
             * Allgemeines Verfahren: Eine Strecke der Länge l soll auf die Länge k geschrumpft werden:
             * - Teilen in k-Fächer der Größe l/k, wenn l/k nicht natürliche Zahl ist, abwechselnd auf/abrunden?
             * - für jedes Fach das Pixel in der Mitte des Fachs nehmen mit (l/2k) (aufrunden)
             * -> allgemein: (i von 1..k){
             *     nehme Pixel i*(l/k)+(l/2k) bzw. (l/k)(i+1/2)
             * }
             * */
            double innerChosenPixel=0; //x-Achse, innere for-Schleife
            double outerChosenPixel=0; //y-Achse- äußere for-Schleife

            bool divisible = (data.Length % goalSize == 0);

            short[,] result = new short[goalSize, goalSize];
            for (int j = 0; j < goalSize; j++) {
                outerChosenPixel = Math.Ceiling((j + 0.5) * (data.GetLength(0) / goalSize) + (divisible ? 0 : (j % 2)));

                for (int i = 0; i < goalSize; i++) {
                    innerChosenPixel = Math.Ceiling((i + 0.5) * (data.GetLength(0) / goalSize) + (divisible ? 0 : (i % 2)));
                    result[j, i] = data[(int)outerChosenPixel, (int)innerChosenPixel];
                }
            }
            return result;
        }

        /// <summary>
        /// Rechnet die Tiefendaten auf Zahlen zwischen 0...CUBE_SIZE runter unter Beachtung der
        /// Minimal- und Maximaltiefe. Werte, die ausserhalb dieser Grenzen liegen, werden entfernt (=0).
        /// </summary>
        /// <param name="data">Tiefendaten als 2D-Array</param>
        /// <param name="minDepth">Minimaltiefe</param>
        /// <param name="maxDepth">Maximaltiefe</param>
        /// <returns>2D-Array mit heruntergerechneten Daten.</returns>
        short[,] FilterAndResizeNumbers(short[,] data, int minDepth, int maxDepth) {

            short[,] result = new short[data.GetLength(0), data.GetLength(1)];
            int span = maxDepth - minDepth;

            for (int j = 0; j < data.GetLength(0); j++) {
                for (int i = 0; i < data.GetLength(1); i++) {
                    if (data[j, i] < minDepth || data[j, i] > maxDepth) {
                        result[j, i] = 0;
                    } else {
                        result[j, i] = (short)(1 + data.GetLength(0) * (data[j, i] - minDepth) / span);
                    }

                }
            }
            return result;
        }

        /// <summary>
        /// Wandelt Tiefendaten in einen String um, damit er zum Arduino-Board gesendet werden kann.
        /// </summary>
        /// <param name="data">Tiefendaten als quadratisches 2D-Array</param>
        /// <returns>Tiefendaten als String</returns>
        String Convert2DToString(short[,] data) {
            String result = "";

            for (int j = 0; j < data.GetLength(0); j++) {
                for (int i = 0; i < data.GetLength(1); i++) {
                    result += data[j, i].ToString();
                }
            }
            
            return result;
        }
        /// <summary>
        /// Im Würfel ist irgendwie die Reihenfolge der Spalten komisch. Es sollte z.B.
        /// 1234 sein, aber stattdessen ist es 2341. Diese Methode vertauscht die betroffenen Spalten und gleicht
        /// diesen "Fehler" wieder aus.
        /// </summary>
        /// <param name="data">Tiefendaten (noch nicht vertauscht) als 2D-Array</param>
        /// <returns>2D-Array mit vertauschten Spalten</returns>
        short[,] SwitchColumns(short[,] data) {
            short[,] result = new short[data.GetLength(0), data.GetLength(1)];
            for (int j = 0; j < data.GetLength(0); j++) {
                for (int i = 0; i < data.GetLength(1); i++) {
                    result[j, (i == 3) ? 0 : (i + 1)] = data[j, i];
                }
            }
            return result;
        }

        /// <summary>
        /// Gibt ein 2D-Array (die Tiefendaten) mit Koordinatensystem aus.
        /// </summary>
        /// <param name="data">Tiefendaten als 2D-Array</param>
        void PrintDepthData(short[,] data) {    //mit "Koordinatensystem"
            for (int j = -1; j < data.GetLength(1); j++) {
                Console.Write("{0,3}", j + " ");
            }
            Console.WriteLine();
            for (int i = 0; i < data.GetLength(0); i++) {
                Console.Write("{0,3}", i + " ");
                for (int j = 0; j < data.GetLength(1); j++) {
                    if (data[i, j] > 100) {
                        Console.Write("{0,3}", data[i, j] / 100 + " ");
                    } else {
                        Console.Write("{0,3}", data[i, j] + " ");
                    }
                }
                Console.Write("{0,3}", i + " ");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Sendet die Tiefendaten an serielle Schnittstelle.
        /// HINWEIS: Hier gibt es noch einen Fehler, dessen Ursache unbekannt ist:
        /// Das Programm bleibt, wenn es ohne Debugger läuft (d.h. wenn man den in der IDE ausschaltet oder nur über .exe startet), manchmal bei port.Close()
        /// ohne ersichtlichen Grund hängen.
        /// 
        /// Update: 
        /// Das Problem besteht darin, dass jedes Mal, wenn ein Signal gesendet wird, der Port aufgemacht, das Signal gesendet und dann der Port wieder
        /// geschlossen wird. Laut Referenz sollte immer etwas Zeit vergehen, bis ein port.open() auf ein port.close() folgt; hier ist es aber immer hintereinander
        /// passiert.
        /// Was nun gemacht wurde: port ist nicht mehr eine Variable dieser Methode, sondern eine Member-Variable der Klasse. Am Anfang (bei ReadSignal) wird nun
        /// der Port einmal geöffnet und bei TurnSensorOff erst wieder geschlossen. Dazwischen bleibt der Port also permanent offen.
        /// </summary>
        /// <param name="data">Zu sendende Daten als String</param>
        public void SendDataString(String data) {

            try {
                //Sende Datenstring mit x als Trennzeichen für die einzelnen Blöcke
                port.Write(data+"x");

            } catch (Exception e) {
                Console.WriteLine("Es ist ein Problem aufgetreten.");
                Console.WriteLine(e);

                //auf Enter warten und dann Programm schliessen
                Console.ReadLine();
                System.Environment.Exit(2);
            }
          
        }

        /// <summary>
        /// Schaltet Sensor aus; kann ausserhalb der Klasse aufgerufen werden, da public. (noch ungenutzt)
        /// </summary>
        public void TurnSensorOff() {
            Console.WriteLine("Sensor wurde ausgeschaltet! Fenster wird gleich geschlossen. Bitte warten...");
            sensor.DepthFrameReady -= VerarbeiteSensorFrameDaten;
            port.Close();
            sensor.Stop();
        }

        /// <summary>
        /// Einfache Sequenz von fiktiven 2D-Array zum Testen der sendDataString-Methode.
        /// </summary>
        public void TestProgramm() {
            int zeit = delayTime;
            String tempString;
            short[,] depthValues = new short[4,4] {
                {4,3,2,1},
                {4,3,2,1},
                {4,3,2,1},
                {4,3,2,1}
            };

            for (int i = 0; i < 100; i++) {
                tempString = Convert2DToString(SwitchColumns(depthValues));
                Console.WriteLine(tempString);
                SendDataString(tempString);
                System.Threading.Thread.Sleep(zeit);
                SendDataString("x");
            }
            Console.WriteLine("Ende");
        }

    }
}
