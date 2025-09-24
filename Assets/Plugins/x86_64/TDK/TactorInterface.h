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


#ifndef _EAITACINT_
#define _EAITACINT_

#include <EAI_Defines.h>

#ifdef WIN32
	#ifdef BUILD_TACTIONINTERFACE_DLL
		#define EXPORTtactionInterface extern "C" __declspec(dllexport)
	#else
		#define EXPORTtactionInterface  extern "C"  __declspec(dllimport)
	#endif
#else
	#define  EXPORTtactionInterface extern "C"
#endif

/****************************************************************************
*FUNCTION: InitializeTI
*DESCRIPTION		Sets up TDK
*
*
*RETURNS:
*			on success:		0
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int InitializeTI();


/****************************************************************************
*FUNCTION: ShutdownTI
*DESCRIPTION		Shuts down and cleans up the TDK
*
*
*RETURNS:
*			on success:		0
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int ShutdownTI();

/****************************************************************************
*FUNCTION: GetVersionNumber
*DESCRIPTION		Version Identification of TDK
*
*
*RETURNS:
*			on success:		value Version Number
*****************************************************************************/
EXPORTtactionInterface
const char* GetVersionNumber();

/****************************************************************************
*FUNCTION: Connect
*DESCRIPTION		Connect to a Tactor Controller
*PARAMETERS
*IN: const char*	_name - Tactor Controller Name (proper name or COM Port)
*IN: int			_type - Tactor Controller Type (see list of types in 	
*IN: void*			_callback - reponse packet return function (can be null) 
*								(see function declaration in 
*
*RETURNS:
*			on success:		Board Identification Number
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int Connect(const char* name, int type, void* _callback);

/****************************************************************************
*FUNCTION: Discover
*DESCRIPTION		Scan Available Ports on computer for Tactor Controller
*PARAMETERS
*IN: int			_type -	Type of Controllers to Scan For (bitfield,
*							multiple types can be ORd together.)
*
*RETURNS:
*			on success:		Number of Devices Found
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int Discover(int type);

/****************************************************************************
*FUNCTION: DiscoverLimited
*DESCRIPTION		Scan Available Ports on computer for Tactor Controller with alotted amount
*PARAMETERS
*IN: int			_type -	Type of Controllers to Scan For (bitfield,
*									multiple types can be ORd together.)
*					_amount - the alotted amount.
*						
*RETURNS:
*			on success:		Number of Devices Found
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int DiscoverLimited(int type,int amount);

/****************************************************************************
*FUNCTION: GetDiscoveredDeviceName
*DESCRIPTION		Scan Available Ports on computer for Tactor Controller
*PARAMETERS
*IN: int			index -	Device To Get Name From (Index from Discover) NOT BOARD ID
*
*RETURNS:
*			on success:		const char* Name
*			on failure:		NULL check GetLastEAIError() for Error Code 
*****************************************************************************/
EXPORTtactionInterface
const char* GetDiscoveredDeviceName(int index);

/****************************************************************************
*FUNCTION: GetDiscoveredDeviceType
*DESCRIPTION		Scan Available Ports on computer for Tactor Controller
*PARAMETERS
*IN: int			index -	Device To Get Name From (Index from Discover) NOT BOARD ID
*
*RETURNS:
*			on success:		integer representing the device discovered's type
*			on failure:		0 check GetLastEAIError() for Error Code 
*****************************************************************************/
EXPORTtactionInterface
int GetDiscoveredDeviceType(int index);

/****************************************************************************
*FUNCTION: Close
*DESCRIPTION		Closes Connection with Selected Device
*PARAMETERS
*IN: int			_deviceID - Tactor Controller Device ID (returned from Connect)
*
*RETURNS:
*			on success:		0
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int Close(int deviceID);

/****************************************************************************
*FUNCTION: CloseAll
*DESCRIPTION		Closes All Active Connections
*
*RETURNS:
*			on success:		0
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int CloseAll();

/****************************************************************************
*FUNCTION: Pulse
*DESCRIPTION		Command Sent to the Tactor Controller
*					Pulses Specified Tactor
*PARAMETERS
*IN: int			_deviceID		- Device To apply Command
*IN: int			_tacNum			- Tactor Number For Command
*IN: int			_msDuration		- Duration of Pulse
*IN: int			_delay			- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int Pulse(int deviceID, int _tacNum, int _msDuration, int _delay);
/****************************************************************************
*FUNCTION: SendActionWait
*DESCRIPTION		Command Sent to the Tactor Controller
*					Waits all actions for given time.
*PARAMETERS
*IN: int			_deviceID		- Device To apply Command
*IN: int			_msDuration		- Duration of Wait
*IN: int			_delay			- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int SendActionWait(int deviceID, int _msDuration, int _delay);

/****************************************************************************
*FUNCTION: ChangeGain
*DESCRIPTION		Command Sent to the Tactor Controller
*					Changes the Gain For Specified Tactor
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_tacNum		- Tactor Number For Command
*IN: int			_gainVal	- Gain Value (0-255)
*IN: int			_delay		- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int ChangeGain(int deviceID, int _tacNum, int gainval, int _delay);

/****************************************************************************
*FUNCTION: RampGain
*DESCRIPTION		Command Sent to the Tactor Controller
*					Changes the Gain From start to end within the duration specified for the tactor number specified
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_tacNum		- Tactor Number For Command
*IN: int			_gainStart	- Start Gain Value (0-255)
*IN: int			_gainEnd	- End Gain Value (0-255)	
*IN: int			_duration	- Duration of Ramp (ms)
*IN: int			_func		- Function Type
*IN: int			_delay		- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int RampGain(int deviceID, int _tacNum, int _gainStart, int _gainEnd,
		int _duration, int _func, int _delay);

/****************************************************************************
*FUNCTION: ChangeFreq
*DESCRIPTION		Command Sent to the Tactor Controller
*					Changes the Frequency For Specified Tactor
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_tacNum		- Tactor Number For Command
*IN: int			_freqVal	- Freq Value (300-3550)
*IN: int			_delay		- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int ChangeFreq(int deviceID, int _tacNum, int freqVal, int _delay);

/****************************************************************************
*FUNCTION: RampFreq
*DESCRIPTION		Command Sent to the Tactor Controller
*					Changes the Freq From start to end within the duration specified for the tactor number specified
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_tacNum		- Tactor Number For Command
*IN: int			_freqStart	- Start Freq Value (300-3550)
*IN: int			_freqEnd	- End Freq Value (300-3550)
*IN: int			_duration	- Duration of Ramp (ms)
*IN: int			_func		- Function Type
*IN: int			_delay		- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int RampFreq(int deviceID, int _tacNum, int _freqStart, int _freqEnd,
		int _duration, int _func, int _delay);

/****************************************************************************
*FUNCTION: ChangeSigSource
*DESCRIPTION		Command Sent to the Tactor Controller
*					Changes the Sig Source For Specified Tactor
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_tacNum		- Tactor Number For Command
*IN: int			_type		- New Sig Source Type
*IN: int			_delay		- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int ChangeSigSource(int _device, int _tacNum, int _type, int _delay);

/****************************************************************************
*FUNCTION: ReadFW
*DESCRIPTION		Command Sent to the Tactor Controller
*					Requests Firmware Version From Tactor Controller
*PARAMETERS
*IN: int			_deviceID		- Device To apply Command
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int ReadFW(int deviceID);

/****************************************************************************
*FUNCTION: TactorSelfTest
*DESCRIPTION		Command Sent to the Tactor Controller
*					Self Test Tactor Controller
*PARAMETERS
*IN: int			_deviceID		- Device To apply Command
*IN: int			_delay			- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int TactorSelfTest(int deviceID, int _delay);

/****************************************************************************
*FUNCTION: ReadSegmentList
*DESCRIPTION		Command Sent to the Tactor Controller
*					Request for Segment List From Tactor Controller
*					Represents Number of Tactors connected to Tactor Controller
*PARAMETERS
*IN: int			_deviceID		- Device To apply Command
*IN: int			_delay			- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int ReadSegmentList(int deviceID, int _delay);

/****************************************************************************
*FUNCTION: ReadBatteryLevel
*DESCRIPTION		Command Sent to the Tactor Controller
*					Request for Battery Level From Tactor Controller
*
*PARAMETERS
*IN: int			_deviceID		- Device To apply Command
*IN: int			_delay			- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int ReadBatteryLevel(int deviceID, int _delay);

/****************************************************************************
*FUNCTION: Stop
*DESCRIPTION		Command Sent to the Tactor Controller
*					Requests Tactor Controller To Stop All Commands
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_delay		- Delay before running Command (ms)
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int Stop(int deviceID, int _delay);

/****************************************************************************
*FUNCTION: SetTactors
*DESCRIPTION		Command Sent to the Tactor Controller
*					Sets the tactors on or off based on the byte array given.
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_delay		- Delay before running Command (ms)
*IN: unsigned char*	states		- An 8-byte array of boolean values representing
*								  the desired tactor states. Tactor 1 is at
*								  the LSB of byte 1, tactor 64 is the MSB
*								  of byte 8.
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int SetTactors(int device_id, int delay, unsigned char* states);

/****************************************************************************
*FUNCTION: SetTactorType
*DESCRIPTION		Command Sent to the Tactor Controller
*					Sets the given tactor to the type described.
*PARAMETERS
*IN: int			device_id	- Device To apply Command
*IN: int			delay		- Delay before running Command (ms)
*IN: int			tactor		- The tactor to modify
*IN: type			type		- The type to set the tactor to
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int SetTactorType(int device_id, int delay, int tactor, int type);

/****************************************************************************
*FUNCTION: UpdateTI
*DESCRIPTION		Update The TactorInterface for house maintence
*					- will check for errors with internal threads
*
*PARAMETERS
*
*RETURNS:
*			on success:		value(0)
*			on failure:		Error code - See EAI_Defines.h for reason
*****************************************************************************/
EXPORTtactionInterface
int UpdateTI();

/****************************************************************************
*FUNCTION: GetLastEAIError
*DESCRIPTION		Returns EAI Error Code (See full list of error codes in EAI_ErrorCodes.h)
*
*PARAMETERS
*
*
*RETURNS:			Last ErrorCode
*****************************************************************************/
EXPORTtactionInterface
int GetLastEAIError();

/****************************************************************************
*FUNCTION: SetLastEAIError
*DESCRIPTION		Sets EAI Error Code (See full list of error codes in EAI_ErrorCodes.h)
*
*PARAMETERS
*IN: int			e the last error code.. ***internal use.
*
*RETURNS:			Last ErrorCode
*****************************************************************************/
EXPORTtactionInterface
int SetLastEAIError(int e);

/****************************************************************************
*FUNCTION: SetTimeFactor
*DESCRIPTION		Set DLL Time Factor to be passed with each Action List
*					10 is the default
*
*PARAMETERS
*IN: int			_timeFactor (1-255) multiple delay value * timefactor for actual delay
*
*RETURNS:
*			on success: 0
*			on failure: -1, and sets last EAI error as ERROR_BADPARAMETER
*
*****************************************************************************/
EXPORTtactionInterface
int SetTimeFactor(int value);

/****************************************************************************
*FUNCTION: BeginStoreTAction
*DESCRIPTION		Sets the tactor controller in the 'recording TAction' mode.
*
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			tacID		- The the address of the TAction to store (1-10)
*
*RETURNS:
*			on success: 0
*			on failure: -1, and sets last EAI error as ERROR_BADPARAMETER
*
*****************************************************************************/
EXPORTtactionInterface
int BeginStoreTAction(int _deviceID, int tacID);

/****************************************************************************
*FUNCTION: FinishStoreTAction
*DESCRIPTION		Finishes the 'recording TAction' mode.
*
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*
*RETURNS:
*			on success: 0
*			on failure: -1, and sets last EAI error as ERROR_BADPARAMETER
*
*****************************************************************************/
EXPORTtactionInterface
int FinishStoreTAction(int _deviceID);

/****************************************************************************
*FUNCTION: PlayStoredTAction
*DESCRIPTION		Plays a TAction stored on the tactor device.
*
*PARAMETERS
*IN: int			_deviceID	- Device To apply Command
*IN: int			_delay		- Delay before running Command (ms)
*IN: int			tacId		- The the address of the TAction to play (1-10)
*
*RETURNS:
*			on success: 0
*			on failure: -1, and sets last EAI error as ERROR_BADPARAMETER
*
*****************************************************************************/
EXPORTtactionInterface
int PlayStoredTAction(int _deviceID, int _delay, int tacId);

/****************************************************************************
*FUNCTION: SetFreqTimeDelay
*DESCRIPTION		Disables or enables the pulse to end on the sin wave reaching
*					zero or the duration of the pulse
*
*PARAMETERS
*IN: int			_deviceID		- Device To apply Command
*IN: int			_delayOn		- False to end with duration, True to end when
*									  sin reaches zero after duration.
*
*RETURNS:
*			on success:		value(0)
*			on failure:		value(-1) check GetLastEAIError() for Error Code
*****************************************************************************/
EXPORTtactionInterface
int SetFreqTimeDelay(int _deviceID, bool _delayOn);
#endif
