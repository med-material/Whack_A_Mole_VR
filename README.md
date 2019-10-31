# Whack-A-Mole VR Game made with Unity
========
![Whack A Mole VR In-Game Screenshot](https://raw.githubusercontent.com/med-material/Whack_A_Mole_VR/master/game.png)
The Whack_A_Mole VR environment provides a test for the Pupil Labs Calibration environment, used with the Pupil Labs plugin for eye-tracking in VR. The main goal
of this project is to be used in clinics for rehabilitation of visual neglected people.
This kind of game helps the patients to focus and think faster, it pushes him to observe more his environment. In addition to the fun of playing the game for the patient, which is
a change compared to other kind of tests which are less funnier, patient who went through rehab with this game started to recognize increasing capabilities in everyday life
The creation of this game in virtual reality and with the use of the Pupil labs plugin will help us more to analyze the patient’s behaviors and reactions. We can also change game’s
parameters such as the apparition’s speed, and also add distractors into it.

Different moles will appear, represented by spheres which will lights in green or red, while producing a sound. With ray from his controller, the
user will have ton point at a mole, which will then shine, and then press the trigger to "Whack" the mole.
The user must not whack the red moles, as it takes off one point (one red moles "whacked").

The incurved wall, in which the moles will appear, is always at the same distance from the user. The user can freely move is head, but if he changes 
his position, the wall will follow.

In the menu, you can ether choose the mode by pressing "1/2/3/4" on the keyboard, or (in some version of Unity) directly click on the mode.
A double click or an enter key pressed will launch the mode.

There is four modes on this game, four differents difficulty modes in which the different parameters are changed:
![Whack A Mole Difficulty Selection](https://raw.githubusercontent.com/med-material/Whack_A_Mole_VR/master/Menu2.png)

In the first three modes, the life time of a mole when it appears is 5s.


## Easy Mode :

A maximum of 2 moles can appear at the same time.

You can miss 5 of them (including if a red mole is whacked which add a missed mole to the count) before game over.

The range of time between two moles'appearances can go from 3s to 4s.


## Medium Mode :

A maximum of 4 moles can appear at the same time.

You can miss 3 of them (including if a red mole is whacked which add a missed mole to the count) before game over.

The range of time between two moles'appearances can go from 1.5s to 3s.


## Hard Mode :

A maximum of 6 moles can appear at the same time.

You can miss 2 of them (including if a red mole is whacked which add a missed mole to the count) before game over.

The range of time between two moles'appearances can go from 0.5s to 1.5s.


## Gradual Mode :

The difficulty of this mode will be gradually increased as the game goes on. At each step (each number of whacked green moles), the parameters are changed.


**Until 4 whacked moles :** 

A maximum of 2 moles can appear at the same time.

You can miss 5 of them (including if a red mole is whacked which add a missed mole to the count) before game over.

The life time of a mole when it appears is 5s.

The range of time between two moles'appearances can go from 2.75s to 3.5s.


**From 4 to 9 whacked moles :** 

Max moles : 4

Max missed : 4

Life time : 4s

Range :  From 2s to 2.75s


**From 9 to 16 whacked moles :** 

Max moles : 6

Max missed : 3

Life time : 3s

Range : From 1.25s to 2s


**From 16 to 24 whacked moles :** 

Max moles : 8

Max missed : 2

Life time : 2s

Range : From 0.5s to 1.25s


**From 24 whacked moles :** 

Max moles : 10

Max missed : 2

Life time : 2s

Range : From 0.1s to 0.8s


At each step, the counters are put down to zero (If you missed 4 moles in the first step, the missed moles count will be zero again at the beginning 
of the second step). The total numbers are saved to be displayed on the game over wall.


When the user misses more moles than the maximum number of missed whole allowed, the game stop and a game over wall appears, in which we can
see the total number of moles whacked, moles missed, and red moles whacked.
![Whack A Mole Game Over Screen](https://raw.githubusercontent.com/med-material/Whack_A_Mole_VR/master/gameover.png)


A lot of things can still be modified, or added (changing the sounds of the red moles/adding a restart and/or retry key/...), this project is the work of less than 1 week.

Last tested in the morning of February the 8th, 2019.

## Mole Whacking Data Collection

This application logs various game datas, from the activated mole position to the position travelled by the participant's laser between two mole poping. The logs are by default saved in the application main directory, however a custom path can be set from the Unity editor.

The logged datas are:

* **TimeStamp**: a time stamp in the dd/mm/yyyy hh:mm:ss.mmm format referencing the instant a given event happened
* **ParticipantId**: the ID of the participant, corresponding to a number set by the Therapist before launching the game
* **TestId**: the ID of the test, corresponding to a number set by the Therapist before launching the game
* **GameId**: a unique ID given to the current game
* **Event**: the name of the event raised
* **TimeSinceLastEvent**: the time spend between this event and the previous one
* **GameState**: the current state of the game (e.g. Game Paused)
* **GameSpeed**: the current speed of the game (e.g. Fast)
* **GameDuration**: the duration in seconds of the game
* **GameTimeSpent**: the time spent since the game started
* **GameTimeLeft**: the time left before the current game ends
* **RightControllerMain**: Is the right controller the main controller (e.g. True, False)
* **MirrorEffect**: Is the mirror effect modifier activated (e.g. True, False)
* **EyePatch**: the eye patch position (e.g. Left, Right, None)
* **PrismEffect**: Is the prism effect activated (e.g. True, False)
* **DualTask**: Is the dual task activated (e.g. True, False)

* **MoleId**: the unique ID of the mole, being a four digits number
* **MoleIndexX**: the X index of the mole
* **MoleIndexY**: the Y index of the mole
* **MolePositionWorld** (X, Y, Z): the world position of the mole concerned by the event, separated in three parameters for each axis
* **MolePositionLocal** (X, Y, Z): the local position of the mole concerned by the event, separated in three parameters for each axis
* **MoleLifeTime**: the duration the mole will stay activated
* **MoleActivatedDuration**: the time the mole spent activated before deactivating/being poped
* **CurrentMoleToHitId**: the unique ID of the current mole to hit, being a four digits number
* **CurrentMoleToHitIndexX**: the X index of the current mole to hit
* **CurrentMoleIndexY**: the Y index of the current mole to hit
* **CurrentMolePositionWorld** (X, Y, Z): the world position of the current mole to hit, separated in three parameters for each axis
* **CurrentMolePositionLocal** (X, Y, Z): the local position of the current mole to hit, separated in three parameters for each axis

* (Right, Left) **ControllerPosWorld** (X, Y, Z): the world position of the right/left controller, separated in three parameters for each axis
* (Right, Left) **ControllerPosLocal** (X, Y, Z): the local position of the right/left controller, separated in three parameters for each axis
* (Right, Left) **ControllerRotEuler** (X, Y, Z): the rotation in euler angles of the right/left controller, separated in three parameters for each axis
* (Right, Left) **ControllerPosTravel** (X, Y, Z): the total distance that the right/left controller travelled since the last time this parameter was logged, separated in three parameters for each axis
* (Right, Left) **ControllerRotTravel** (X, Y, Z): the total angle that the right/left controller travelled since the last time this parameter was logged, separated in three parameters for each axis
* **HeadCameraPosWorld** (X, Y, Z): the world position of the head camera, separated in three parameters for each axis
* **HeadCameraPosLocal** (X, Y, Z): the local position of the head camera, separated in three parameters for each axis
* **HeadCameraRotEuler** (X, Y, Z): the rotation in euler angles of the head camera, separated in three parameters for each axis
* **HeadCameraPosTravel** (X, Y, Z): the total distance that the head camera travelled since the last time this parameter was logged, separated in three parameters for each axis
* **HeadCameraRotTravel** (X, Y, Z): the total angle that the head camera travelled since the last time this parameter was logged, separated in three parameters for each axis
* (Right, Left) **ControllerLaserPosWorld** (X, Y, Z): the world position of the right/left controller's laser, separated in three parameters for each axis
* (Right, Left) **ControllerLaserPosLocal** (X, Y, Z): the local position of the right/left controller's laser, separated in three parameters for each axis
* (Right, Left) **ControllerLaserRotEuler** (X, Y, Z): the rotation in euler angles of the right/left controller's laser, separated in three parameters for each axis
* (Right, Left) **ControllerLaserPosTravel** (X, Y, Z): the total distance that the right/left controller's laser travelled since the last time this parameter was logged, separated in three parameters for each axis


Installation
------------

For now, the Pupil Labs eye-tracking plugin has not been integrated to the application. You just have to launch it via the Unity Editor.

