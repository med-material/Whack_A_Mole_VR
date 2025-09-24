/*
	This software is the copyrighted work of Engineering Acoustics, Inc. and/or its suppliers and may not be 
	reproduced or redistributed without prior written permission.
	Engineering Acoustics, Inc (EAI) provides this software “as is," and use the software is at your own risk.  
	EAI make no warranties as to performance, merchantability, fitness for a particular purpose, or any 
	other warranties whether expressed or implied. Under no circumstances shall EAI be liable for direct, 
	indirect, special, incidental, or consequential damages resulting from the use, misuse, or inability to use 
	this software, even if EAI has been advised of the possibility of such damages.
	
	Copyright 2015(c) Engineering Acoustics, Inc. All rights reserved.
*/

#ifndef TACTION_INTERFACE_H_
#define TACTION_INTERFACE_H_


//#undef TACTIONSYSTEM

#ifdef WIN32
#pragma warning(disable : 4996)// _CRT_SECURE_NO_WARNINGS
#ifdef BUILD_TACTIONINTERFACE_DLL
#define EXPORTtactionInterface extern "C" __declspec(dllexport)
#else
#define EXPORTtactionInterface  extern "C"  __declspec(dllimport)
#endif
#else
#define  EXPORTtactionInterface extern "C"
#endif


#ifdef TACTIONSYSTEM

/****************************************************************************
*FUNCTION: LoadTActionDatabase
*DESCRIPTION		Loads the TAction Database file "tactionFile"
*PARAMETERS
*IN: char*			tactionFile - the filename of the sqlite database to load
*
*RETURNS:
*			on success:		Number of TActions Found
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int LoadTActionDatabase(char * tactionFile);

/****************************************************************************
*FUNCTION: IsDatabaseLoaded
*DESCRIPTION		Determines if there is a database currently loaded
*
*RETURNS:
*			on success:		1. there is a database currently loaded
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int IsDatabaseLoaded();

/****************************************************************************
*FUNCTION: GetLoadedTActionSize
*DESCRIPTION		Gets how many TActions are currently loaded.
*
*RETURNS:
*			on success:		Number of TActions Loaded
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int GetLoadedTActionSize();

/****************************************************************************
*FUNCTION: GetTActionDuration
*DESCRIPTION		Gets the total time in milliseconds that it takes for a TAction to complely play.
*PARAMETERS
*IN: int			tacID - the tacID of the TAction to be measured
*
*RETURNS:
*			on success:		The total duration of the TAction in milliseconds
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int GetTActionDuration(int tacID);

/****************************************************************************
*FUNCTION: UnloadTActions
*DESCRIPTION		Unloads all the currently loaded TActions
*
*RETURNS:
*			on success:		0
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int UnloadTActions();

/****************************************************************************
*FUNCTION: GetTActionName
*DESCRIPTION		Gets the name of a specified TAction
*PARAMETERS
*IN: int			tacID - The ID of the TAction to get the name of.
*
*RETURNS:
*			on success:		the name of the TAction.
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
char* GetTActionName(int tacID);

/****************************************************************************
*FUNCTION: CanTActionMap
*DESCRIPTION		Determines if a taction can map to the given tactor device.
*PARAMETERS
*IN: int			DeviceID - the device id to play the TAction on.
int					tacID - the id of the TAction to play.
int					tactorID - the id of the tactor to play the TAction on.

*
*RETURNS:
*			on success:		0 successfully mapped and played.
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int CanTActionMap(int boardID, int tacID, int tactorID);

/****************************************************************************
*FUNCTION: PlayTAction
*DESCRIPTION		Plays the given taction to the given controller with the given scalefactors.
*PARAMETERS
*IN: int			DeviceID - the device id to play the TAction on.
	 int			tacID - the id of the TAction to play.
	 int			tactorID - the id of the tactor to play the TAction on.
	 float			gainScale - how much to scale the entire gain of the TAction by.
	 float			freq1 - how much to scale the entire freq1 of the TAction by.
	 float			freq2 - how much to scale the entire freq2 of the TAction by.
	 float			timeScale - how much to scale the entire time of the TAction by.
		
*
*RETURNS:
*			on success:		0 successfully mapped and played.
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int PlayTAction(int boardID, int tacID, int tactorID, float gainScale, float freq1Scale, float freq2Scale, float timeScale);

/****************************************************************************
*FUNCTION: PlayTActionToSegment
*DESCRIPTION		Plays the given taction to the given controller segment with the given scalefactors.
*PARAMETERS
*IN: int			DeviceID - the device id to play the TAction on.
	 int			tacID - the id of the TAction to play.
	 int			tactorIDOffset - the id of the tactor to play the TAction on within the segment
	 int			controllerSegmentID - the segment ID of the controller segment list to map to.
	 float			gainScale - how much to scale the entire gain of the TAction by.
	 float			freq1 - how much to scale the entire freq1 of the TAction by.
	 float			freq2 - how much to scale the entire freq2 of the TAction by.
	 float			timeScale - how much to scale the entire time of the TAction by.
		
*
*RETURNS:
*			on success:		0 successfully mapped and played.
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int PlayTActionToSegment(int boardID, int tacID, int tactorIDOffset,int controllerSegmentID, float gainScale, float freq1Scale, float freq2Scale, float timeScale);

#endif
#endif
