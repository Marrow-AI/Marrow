// MARROW
// JAN 15th 2020
// PASCALE T.

// LIBRAIRIES

#include "Adafruit_Thermal.h"
#include "text.h"
#include "SoftwareSerial.h"
#include "RTClib.h"
#include "pitches.h"

// PRINTER

#define TX_PIN1 6 // RX sur l'imprimante 1 (BLUE)
#define RX_PIN1 5 // TX sur l'imprimante 1 (GREEN)

SoftwareSerial mySerial1(RX_PIN1, TX_PIN1);
Adafruit_Thermal printer(&mySerial1);

// BUTTON

#define buttonPin 8
int debounce = 100;
int currentButtonState = HIGH;
int previousButtonState;
int time;

// DRAW
#define numberOption 4
int choices[4];
int choicesMade;

// RING

int melody[] = {
  NOTE_A4, NOTE_FS4
};

// BUZZER 

int noteDurations[] = {
  2, 2
};

// TESTING

int t = 1;

void setup() {

  Serial.begin(9600);

  // PRINTER

  mySerial1.begin(9600);
  printer.begin();
  printer.justify('L');
  printer.setLineHeight(32);

  // BUTTON

  pinMode(buttonPin, INPUT_PULLUP);
  time = millis();

  // DRAW
  randomSeed(analogRead(A0));
  resetNames();

}

void loop() {

  currentButtonState = digitalRead(buttonPin);

  if (currentButtonState == HIGH && previousButtonState == LOW && millis() - time > debounce) {

    int famMember = selectRandomNameFromRemaining();
    int noteDuration = 1000 / noteDurations[0];
    tone(9, melody[0], noteDuration);
    delay(noteDuration * 1.3);
    noteDuration = 1000 / noteDurations[1];
    tone(9, melody[1], noteDuration);
    delay(noteDuration * 1.3);

    if (famMember == -1)
    {
      printer.println(F("************"));
      resetNames();
      int famMember = selectRandomNameFromRemaining();
    } else
    {

      // TITLE : NAME OF CHARACTER

      printer.setLineHeight();
      printer.justify('L');
      printer.println();
      printer.boldOn();
      printer.setSize('L');
      printer.println(character[famMember]);
      //      printer.println(character[t]);

      printer.println();

      //TEXT : SAME FOR EVERY CHARACTER

      printer.setLineHeight(50);
      printer.boldOff();
      printer.setSize('S');
      printer.println("In this house, each member of   the family plays an Artificial  Intelligence model. You are the");
      //      printer.println("test");
      delay(200);

      //TEXT : SPECIFIC TO EVERY CHARACTER (BOLD)

      printer.setLineHeight(50);
      printer.setSize('S');
      printer.underlineOn();
      printer.println(bold[famMember]);
      //      printer.println(bold[t]);

      // printer.println("test");
      // printer.println();


      //TEXT SPECIFIC TO CHARACTER (NORMAL)

      printer.setLineHeight(50);
      printer.underlineOff();
      printer.setSize('S');
      printer.println(description[famMember]);
      // printer.println(description[t]);
      printer.println();


      //TEXT SPECIFIC TO CHARACTER (NORMAL : NEW LINE )

      printer.underlineOff();
      printer.setSize('S');
      printer.print(description1[famMember]);
      //printer.print(description1[t]);
      //      printer.println("test");
      //      printer.println();


      //TEXT SPECIFIC TO CHARACTER (UNDERLINED)

      printer.underlineOn();
      printer.setSize('S');
      printer.print(underlined[famMember]);
      //printer.print(underlined[t]);


      //TEXT SPECIFIC TO CHARACTER (END NORMAL);

      printer.underlineOff();
      printer.setSize('S');
      printer.println(description2[famMember]);
      //      printer.print(description2[t]);

      printer.println();

      //TEXT COMMON TO EVERY CHARACTER : CENTERED

      printer.boldOff();
      printer.justify('C');
      printer.setSize('S');
      printer.println();
      printer.println("You may share this with the rest of your family");
      printer.println();

      ascii();

      printer.println();
      printer.println();

    }
  }

  previousButtonState = currentButtonState;

}

int selectRandomNameFromRemaining()
{
  if (choicesMade == 4)
  {
    return -1;
  }
  int selection = random(choicesMade, 4);
  int temp = choices[choicesMade];
  choices[choicesMade] = choices[selection];
  choices[selection] = temp;
  return choices[choicesMade++]; //moved incrementing choicesMade to here
}

bool resetNames()
{
  for (int i = 0; i < 4; i++)
  {
    choices[i] = i;
  }
  for (int i = 0; i < 4; i++)
  {
    int index = random(i, 4);
    int temp = choices[i];
    choices[i] = choices[index];
    choices[index] = temp;
  }
  choicesMade = 0;
}

void ascii() {

  printer.justify('C');
  printer.setLineHeight(10);

  for (int i = 0; i < 7; i++) {
    printer.print("  \xDB");
  }
  printer.println("  \xDB");

  for (int j = 0; j < 7; j++) {
    printer.print("\xDB\xDB "); //xDB == black_square
  }
  printer.println("\xDB\xDB ");

}
