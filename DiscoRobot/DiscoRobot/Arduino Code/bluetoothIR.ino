//Eric Butt and Clarisse Vamos
//EEL 4660 - Robotic Systems
//Portions of code adapted from Elegoo tutorials for line tracking, Blutooth, and IR 

#include <IRremote.h>
#include <IRremoteInt.h>
#include <ir_Lego_PF_BitStreamEncoder.h>

//  Pin Constants
int receiverpin = 12;
int in1=6;
int in2=7;
int in3=8;
int in4=9;
int ENA=5;
int ENB=11;
int ABS=150;
unsigned long RES;

//Define arrows and ok
#define UP 16736925
#define DOWN 16754775
#define OK 16712445
#define LEFT 16720605
#define RIGHT 16761405

//Define numbers
#define ONE 16738455
#define TWO 16750695
#define THREE 16756815
#define FOUR 16724175
#define FIVE 16718055
#define SIX 16743045
#define SEVEN 16716015
#define EIGHT 16726215
#define NINE 16734885
#define ZERO 16730805

//Define star and hash
#define STAR 16728765
#define HASH 16732845

//  Define the serial input strings
#define ZIGZAG 'Z'
#define CIRCLE_RIGHT 'C'
#define CIRCLE_LEFT 'D'
#define TURN_RIGHT 'R'
#define TURN_LEFT 'L'
#define FORWARD 'F'
#define BACKWARDS 'B'
#define STOP 'X'
#define SWITCH_TO_IR 'I'

char val;               //  For saving the Serial input
bool useBluetooth;      //  Whether or not to rely on Bluetooth or IR for control input

IRrecv irrecv(receiverpin); //  IR pin
decode_results results;     //  decode the IR results

//  Move the robot forward
void _mForward()
{ 
  digitalWrite(ENA,HIGH);
  digitalWrite(ENB,HIGH);
  digitalWrite(in1,HIGH);//digital output
  digitalWrite(in2,LOW);
  digitalWrite(in3,LOW);
  digitalWrite(in4,HIGH);
  Serial.println("Go Forward!"); 
}

//  Move the robot backwards
void _mBack()
{
  digitalWrite(ENA,HIGH);
  digitalWrite(ENB,HIGH);
  digitalWrite(in1,LOW);
  digitalWrite(in2,HIGH);
  digitalWrite(in3,HIGH);
  digitalWrite(in4,LOW);
  Serial.println("Backward!"); 
}

//  Tur the robot left
void _mLeft()
{
  analogWrite(ENA,ABS);
  analogWrite(ENB,ABS);
  digitalWrite(in1,HIGH);
  digitalWrite(in2,LOW);
  digitalWrite(in3,HIGH);
  digitalWrite(in4,LOW); 
  Serial.println("Go Left!"); 
}

//  Turn the robot right
void _mRight()
{
  analogWrite(ENA,ABS);
  analogWrite(ENB,ABS);
  digitalWrite(in1,LOW);
  digitalWrite(in2,HIGH);
  digitalWrite(in3,LOW);
  digitalWrite(in4,HIGH);
  Serial.println("Go Right!"); 
}

// Stop the robot
void _mStop()
{
  digitalWrite(ENA,LOW);
  digitalWrite(ENB,LOW);
  Serial.println("STOP!");  
}


void setup() {
  pinMode(in1,OUTPUT);
  pinMode(in2,OUTPUT);
  pinMode(in3,OUTPUT);
  pinMode(in4,OUTPUT);
  pinMode(ENA,OUTPUT);
  pinMode(ENB,OUTPUT);
  pinMode(receiverpin,INPUT);
  Serial.begin(9600); 
  delay(50);
  _mStop();
   irrecv.enableIRIn();
  useBluetooth = true;
}

void loop() {

  //  THE BLUETOOTH PORTION
  if (useBluetooth)
  {
    if( Serial.available() ) // if data is available to read
    {
        val = Serial.read(); // read it and store it in 'val'

        if (val == ZIGZAG){
          //  The ZigZag Procedure
          _mRight();
          delay(150);
          _mForward();
          delay(300);
          _mLeft();
          delay(150);
          _mForward();
          delay(300);
          _mRight();
          delay(75);
          _mForward();
          delay(300);
          _mStop();
        }
        else if (val == CIRCLE_RIGHT){
         _mRight();
          delay(1000);
        }
        else if (val == CIRCLE_LEFT){
           _mLeft();
           delay(1000);
        }
        else if (val == TURN_RIGHT){
          _mRight();
        }
        else if (val == TURN_LEFT){
          _mLeft();
        }
        else if (val == FORWARD){
          _mForward();
        }
        else if (val == BACKWARDS){
          _mBack();
        }
        else if (val == STOP){
          _mStop();
        }
        /*
        else if (val == SWITCH_TO_IR){
          _mStop();
          useBluetooth = false;
        }
        */
     }

     // THE IR PORTION
     else //  if useBluetooth == false
     {
        irrecv.resume();
        if (irrecv.decode(&results))
        {
          RES = results.value;
          Serial.println(RES);
          //irrecv.resume();
          delay(150);

          //  Go Forwards
          if (RES == UP){
            _mForward();
            delay(500);
            _mStop();
          }

          //  Bo Backwards
          else if (RES == DOWN){
            _mBack();
            delay(500);
            _mStop();
          }

          //  Turn Left
          else if (RES = LEFT){
            _mLeft();
            delay(500);
            _mStop();
          }

          //  Turn Right
          else if (RES == RIGHT){
            _mRight();
            delay(500); 
          }

          //  Stop
          else if (RES == OK){
            _mStop();
          }

          //  Left Circles
          else if (RES == ONE) {
            _mLeft();
            delay(1500);
            _mStop();
          }

          //  Right Circles
          else if (RES == TWO){
            _mRight();
            delay(1500);
            _mStop();
          }

          //  Zig Zag
          else if (RES ==THREE){
            _mRight();
            delay(150);
            _mForward();
            delay(300);
            _mLeft();
            delay(150);
            _mForward();
            delay(300);
            _mRight();
            delay(75);
            _mForward();
            delay(300);
            _mStop();
          }

          else if (RES == STAR){
            useBluetooth = true;
          }

        }
     }
  }
}

