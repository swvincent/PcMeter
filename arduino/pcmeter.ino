/*
    PC Meter 2
    
    Drives PC Meter device.
    http://www.swvincent.com/pcmeter

    Written in 2018 by Scott W. Vincent
    http://www.swvincent.com
    Email: my first name at swvincent.com

    This is an update to my original PC Meter program from 2013. This new version provides smoother movement
    of the CPU meter, overhauls the serial communcation and has better variable names than before. I've also
    removed use of the delay function and other improvements. I'm sure there's still room for more :-)

    Developed and Tested on an Arduino Leonardo with IDE 1.8.5
  
    Serial communcation code from/based on Robin2's tutorial at:
    http://forum.arduino.cc/index.php?topic=396450.0
  
    Thanks to Hayden Thring for his analog PC meter project inspired by my original Arduino program,
    which has in turn inspired this one. Some ideas on how to further smooth the meter movement
    came from studying and using his code. https://hackaday.io/project/10629-pc-analog-panel-meters-w-arduino
    
    To the extent possible under law, the author has dedicated all copyright and related and neighboring rights to this
    software to the public domain worldwide. This software is distributed without any warranty. 

    You should have received a copy of the CC0 Public Domain Dedication along with this software.
    If not, see <http://creativecommons.org/publicdomain/zero/1.0/>. 
*/

//Constants
const int METER_PINS[2] = {11, 10};     // Meter output pins
const int METER_MAX[2] = {246, 248};    // Max value for meters
const int GREEN_LEDS[2] = {4, 2};       // Green LED pins
const int RED_LEDS[2] = {5, 3};         // Red LED pins
const int RED_ZONE_PERC = 80;           // Percent at which LED goes from green to red
const int METER_UPDATE_FREQ = 100;      // Frequency of meter updates in milliseconds
const long SERIAL_TIMEOUT = 2000;       // How long to wait until serial "times out"
const int READINGS_COUNT = 20;          // Number of readings to average for each meter

//Variables
const byte numRecChars = 32;            // Sets size of receive buffer
char receivedChars[numRecChars];        // Array for received serial data
bool newData = false;                   // Indicates if new data has been received
unsigned long lastSerialRecd = 0;       // Time last serial recd
unsigned long lastMeterUpdate = 0;      // Time meters last updated
int lastValueReceived[2] = {0, 0};      // Last value received
int valuesRecd[2][READINGS_COUNT];      // Readings to be averaged
int runningTotal[2] = {0, 0};           // Running totals
int valuesRecdIndex = 0;                // Index of current reading

void setup() {
  Serial.begin(9600);
  
  //Setup pin modes
  pinMode(METER_PINS[0], OUTPUT);
  pinMode(METER_PINS[1], OUTPUT);
  pinMode(GREEN_LEDS[0], OUTPUT);
  pinMode(RED_LEDS[0], OUTPUT);
  pinMode(GREEN_LEDS[1], OUTPUT);
  pinMode(RED_LEDS[1], OUTPUT);
 
  //Init values Received array
  for (int counter = 0; counter < READINGS_COUNT; counter++)
  {
    valuesRecd[0][counter] = 0;
    valuesRecd[1][counter] = 0;
  }
  
  meterStartup();
 
  //Get times started
  lastMeterUpdate = millis();
  lastSerialRecd = millis();
}

void loop() {
  receiveSerialData();
  updateStats();
  updateMeters();
  screenSaver();
}


void receiveSerialData()
{
    // This is the recvWithEndMarker() function
    // from Robin2's serial data tutorial
    static byte ndx = 0;
    char endMarker = '\r';
    char rc;
   
    while (Serial.available() > 0 && newData == false)
      {
        rc = Serial.read();

        if (rc != endMarker)
        {
          receivedChars[ndx] = rc;
          ndx++;
          if (ndx >= numRecChars)
          {
            ndx = numRecChars - 1;
          }
        }
        else
        {
          receivedChars[ndx] = '\0'; // terminate the string
          ndx = 0;
          newData = true;
        }
    }
}


void updateStats()
{
  if (newData == true)
  {
    switch (receivedChars[0])
    {
      case 'C':
        //CPU
        lastValueReceived[0] = min(atoi(&receivedChars[1]), 100);
        break;
      case 'M':
        //Memory
        lastValueReceived[1] = min(atoi(&receivedChars[1]), 100);
        break;
    }
    
    //Update last serial received
    lastSerialRecd = millis();

    //Ready to receive again
    newData = false;
  }
}


//Update meters and running stats
void updateMeters()
{
  unsigned long currentMillis = millis();
  
  if (currentMillis - lastMeterUpdate > METER_UPDATE_FREQ)
  {
    //Update both meters
    int i;
    for(i = 0; i < 2; i++)
    {
      int perc = 0;

      //Based on https://www.arduino.cc/en/Tutorial/Smoothing
      runningTotal[i] = runningTotal[i] - valuesRecd[i][valuesRecdIndex];
      valuesRecd[i][valuesRecdIndex] = lastValueReceived[i];
      runningTotal[i] = runningTotal[i] + valuesRecd[i][valuesRecdIndex];
      perc = runningTotal[i] / READINGS_COUNT;
      
      setMeter(METER_PINS[i], perc, METER_MAX[i]);
      setLED(GREEN_LEDS[i], RED_LEDS[i], perc, RED_ZONE_PERC);
    }
    
    //Advance index
    valuesRecdIndex = valuesRecdIndex + 1;
    if (valuesRecdIndex >= READINGS_COUNT)
      valuesRecdIndex = 0;

    lastMeterUpdate = currentMillis;
  }
}


//Set Meter position
void setMeter(int meterPin, int perc, int meterMax)
{
  //Map perc to proper meter position
  int pos = map(perc, 0, 100, 0, meterMax);
  analogWrite(meterPin, pos);
}


//Set LED color
void setLED(int greenPin, int redPin, int perc, int redPerc)
{
  int isGreen = (perc < redPerc);
  digitalWrite(greenPin, isGreen);
  digitalWrite(redPin, !isGreen);
}


//Max both meters on startup as a test
void meterStartup()
{
 setMeter(METER_PINS[0], 100, METER_MAX[0]);
 setMeter(METER_PINS[1], 100, METER_MAX[1]);
 digitalWrite(RED_LEDS[0], true);
 digitalWrite(RED_LEDS[1], true);
 //Okay, yes I use delay here, but it's a startup
 //routine, nothing is really happening yet!
 delay(2000);
}


//Move needles back and forth to show no data is
//being received. Stop once serial data rec'd again.
void screenSaver()
{
  if (millis() - lastSerialRecd > SERIAL_TIMEOUT)
  {
    int aPos = 0;
    int bPos = 0;
    int incAmt = 0;
    unsigned long lastSSUpdate = millis();

    //Turn off all LEDs
    digitalWrite(GREEN_LEDS[0], false);
    digitalWrite(GREEN_LEDS[1], false);
    digitalWrite(RED_LEDS[0], false);
    digitalWrite(RED_LEDS[1], false);
    
    while (Serial.available() == 0)
    {
      unsigned long currentMillis = millis();

      //Update every 100ms
      if (currentMillis - lastSSUpdate > 100)
      {
        //B meter position is opposite of A meter position
        bPos = 100 - aPos;
        
        //Move needles
        setMeter(METER_PINS[0], aPos, METER_MAX[0]);
        setMeter(METER_PINS[1], bPos, METER_MAX[1]);
       
        //Change meter direction if needed.
        if (aPos == 100)
          incAmt = -1;
        else if (aPos == 0)
          incAmt = 1;
         
        //Increment position
        aPos = aPos + incAmt;

        lastSSUpdate = currentMillis;
      }
    }
  }
}
