// Definition der Würfelgröße
#define CUBESIZE 4

// Array zum initialisieren der LEDs 
int LEDPin[] = { 
  0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

// Pins zum initialisieren der Ebenen
int LayerPin[] = { 
  16, 17, 18, 19 };
  
//Wie lange ein Muster angezeigt wird; muss mindestens so groß wie delayPerLayer*CUBE_SIZE sein, Minimum also 12
//gute Werte: 60 (Flackern schwächer wahrgenommen als sonst), 90 (geht noch), vielleicht auch 100
int timePerPattern=60;
int delayPerLayer=3;

// Diese Methode wird einmalig aufgerufen und dient zur Initialisierung
void setup() {
  Serial.begin(9600);	// Initialisiert die Übertragung  
  Serial.flush();		// löscht den Eingabepuffer
  
  // LED's auf OUTPUT setzen
  for (int pin = 0; pin < 16; pin++) {
    pinMode(LEDPin[pin], OUTPUT);
    digitalWrite(LEDPin[pin], LOW);
  }

  // Ebenen auf OUTPUT setzen
  for (int layer = 0; layer < 4; layer++) {
    pinMode(LayerPin[layer], OUTPUT);
    digitalWrite(LayerPin[layer], HIGH);
  }
  
  // Pin 19 ist für den seriellen Input zuständig (die letzte Ebene ist somit deaktiviert)
  pinMode(LayerPin[3], INPUT);
}

void loop() {
/* In der Loop Methode wird der Input analysiert und die entsprechende LED aktiviert. Anzumerken ist, dass ein Feld öfters gezeichnet wird und somit nicht dauerhaft neue Daten verarbeitet werden (Delay 60ms) */

  if (Serial.available() >= 16) {

    char array[16];
    for (int i=0; i<16; i++)
    {
      array[i] = Serial.read();  // Einlesen der gelieferten Daten
    }

    
    for (int i=0; i<timePerPattern/(delayPerLayer*CUBESIZE); i++){
      draw(array);
    }

    //falls überflüssige Zeichen vorkommen werden diese solange gelesen, bis ein x kommt
    while(Serial.read()!='x'){
      
    }
  }
}

void draw(char array[]){
  /******************************************************************
   * 	 Würfel wird durchiteriert und
   * 	 LEDs werden entsprechend des eingehenden Characters verarbeitet
   * 	 Ebenen:  LOW aktiviert, HIGH deaktiviert
   * 	 LEDs:    HIGH aktiviert, LOW deaktiviert
   	 ******************************************************************/
  int i=0;
  for (int layer = 0; layer < 4; layer++)     // gehe die Ebenen durch
  {
    //Deaktiviere die Ebene, die im vorigen Schleifendurchlauf aktiviert wurde

    if(layer==0)
      digitalWrite(LayerPin[3], HIGH);  	  // Ebene deaktivieren
    else
      digitalWrite(LayerPin[layer-1], HIGH);  // Ebene deaktivieren

    // alle LEDs in aktueller Ebene aubschalten
    for (int pin = 0; pin < 16; pin++)    	  // alle LEDs aus machen
    { 
      digitalWrite(LEDPin[pin], LOW);
    }

    //aktuelle Ebene aktivieren
    digitalWrite(LayerPin[layer], LOW);     

    //gehe durch alle "Spalten" einer Ebene
    // =========================================
	//j zurücksetzen, damit es wieder in der linken Spalte beginnt
	int j=0; 

    for (int digit = 0; digit < 4; digit++)   // gehe die Eingabeziffern durch
    {
      char value = array[i]; // hole aktuelle Ziffer aus dem Array  

      if (value == '1')
        digitalWrite(LEDPin[0 + j], HIGH);    // LED in vorderster Reihe anmachen
      else if (value == '2')
        digitalWrite(LEDPin[1 + j], HIGH); 	  // LED in 2. Reihe anmachen
      else if (value == '3')
        digitalWrite(LEDPin[2 + j], HIGH); 	  // LED in 3. Reihe anmachen
      else if (value == '4')
        digitalWrite(LEDPin[3 + j], HIGH);    // LED in letzter Reihe anmachen


      i++;    	// Array zählt insgesamt um 4 hoch (entspricht einer Ebene)
      j += 4; 	// erforderlich, damit alle 16 LEDs angesteuert werden (und nicht nur 4) - "springt" eine 4er Reihe im Würfel weiter
    }
    delay(delayPerLayer);	// legt fest wie lange jede einzelne LED leuchtet
  }
}

