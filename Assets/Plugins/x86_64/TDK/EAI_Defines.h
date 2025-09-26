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

/************************************************************************
*                                                                       *
*   EAI_Defines.h --  error code definitions for the EAI Software       *
*                                                                       *
*   Copyright 2015(c) Engineering Acoustics Inc. All rights reserved.   *
*                                                                       *
************************************************************************/


#ifndef _EAIDEFINES_
#define _EAIDEFINES_

// Default value to play a TAction at it's default location
#define TACTION_LOCATION_DEFAULT		0xDF

// Number of device types.
#define DEVICE_TYPE_COUNT				5

// Flags for device types!
// Change in 0.1.0.7: These were integers, and are now bitflags!
#define DEVICE_TYPE_UNKNOWN				0x00
#define DEVICE_TYPE_SERIAL				0x01
#define DEVICE_TYPE_WINUSB				0x02
#define DEVICE_TYPE_ANDROIDBLUETOOTH	0x04
#define DEVICE_TYPE_ANDROIDUSB			0x08
#define DEVICE_TYPE_ANDROIDBLE			0x10

#define TDK_LINEAR_RAMP					0x01

// values for command bytes
#define TDK_COMMAND_PULSE				0x11
#define TDK_COMMAND_READFW				0x42
#define TDK_COMMAND_READ_CURRENT		0x53
#define TDK_COMMAND_STOP				0x01
#define TDK_COMMAND_GAIN				0x20
#define TDK_COMMAND_FREQ				0x12
#define TDK_COMMAND_SETSIGSOURCE		0x15
#define TDK_COMMAND_RAMP				0x2A
#define TDK_COMMAND_SELFTEST			0x30
#define TDK_COMMAND_READ_BAT_DATA		0x3a
#define TDK_COMMAND_GETSEGMENTLIST		0x45
#define TDK_COMMAND_SET_TACTORS			0x80
#define TDK_COMMAND_SET_TACTOR_TYPE		0x33
#define TDK_COMMAND_TACTION_PLAY		0x1A
#define TDK_COMMAND_TACTION_START		0x1B
#define TDK_COMMAND_ACTION_WAIT			0x1F
//#define TDK_COMMAND_TACTION_???		0x1C
#define TDK_COMMAND_TACTION_END			0x1D
#define TDK_COMMAND_SET_FREQ_TIME_DELAY	0xA2

// SetSigSource type values. Can be bit-or'd together.
#define TDK_SIG_SRC_PRIMARY				0x01
#define TDK_SIG_SRC_MODULATION			0x02
#define TDK_SIG_SRC_NOISE				0x04

// Pre-done SigSource combinations.
#define TDK_SIG_SRC_PRIMARY_AND_MOD		(TDK_SIG_SRC_PRIMARY | TDK_SIG_SRC_MODULATION)
#define TDK_SIG_SRC_PRIMARY_AND_NOISE	(TDK_SIG_SRC_PRIMARY | TDK_SIG_SRC_NOISE)
#define TDK_SIG_SRC_MOD_AND_NOISE		(TDK_SIG_SRC_MODULATION | TDK_SIG_SRC_NOISE )
#define TDK_SIG_SRC_PRIMARY_MOD_NOISE	(TDK_SIG_SRC_PRIMARY | TDK_SIG_SRC_MODULATION | TDK_SIG_SRC_NOISE)


#define MAX_ACTION_DURATION 2500	// the maximum length of an action duration
#define MIN_ACTION_DURATION 10		// the minimum length of an action duration.
#define MAX_ACTION_FREQUENCY 3500	// the maximum frequency of an action.
#define MIN_ACTION_FREQUENCY 300	// the minimum frequency of an action.
#define MAX_ACTION_GAIN 255			// the maximum gain of an action.
#define MIN_ACTION_GAIN 1			// the minimum gain of an action.

#define TDK_MAX_STORED_TACTIONS 10			// the number of TActions the device can hold
#define TDK_MAX_STORED_TACTION_LENGTH 64	// the number of actions a stored TAction can h old

// tactor type values
#define TDK_TACTOR_TYPE_C3				0x11
#define TDK_TACTOR_TYPE_C2				0x12
#define TDK_TACTOR_TYPE_EMS				0x21
#define TDK_TACTOR_TYPE_EMR				0x22

#define ERROR_NOINIT									202000
#define ERROR_CONNECTION								202001
#define ERROR_BADPARAMETER								202002
#define ERROR_INTERNALERROR								202003
#define ERROR_PARTIALREAD								202004
#define ERROR_HANDLE_NULL								202005
#define ERROR_WIN_ERROR									202006
#define ERROR_EAITIMEOUT								202007
#define ERROR_EAINOREAD									202008
#define ERROR_FAILED_TO_CLOSE							202009
#define ERROR_MORE_TO_READ								202010
#define ERROR_FAILED_TO_READ							202011
#define ERROR_FAILED_TO_WRITE							202012
#define ERROR_NO_SUPPORTED_DRIVER						202013

#define ERROR_PARSE_ERROR								203000

#define ERROR_DM_ACTION_LIMIT_REACHED					204010
#define ERROR_DM_FAILED_TO_GENERATE_DEVICE_ID			204011

#define ERROR_JNI_UNKNOWN								205000
#define ERROR_JNI_BAD									205001
#define ERROR_JNI_FIND_CLASS_ERROR						205002
#define ERROR_JNI_FIND_FIELD_ERROR						205003
#define ERROR_JNI_FIND_METHOD_ERROR						205004
#define ERROR_JNI_CALL_METHOD_ERROR						205005
#define ERROR_JNI_RESOURCE_ACQUISITION_ERROR			205006
#define ERROR_JNI_RESOURCE_RELEASE_ERROR				205007

#define ERROR_SI_ERROR									302000

#define ERROR_TM_NOT_INITIALIZED						402000
#define ERROR_TM_NO_DEVICE								402001
#define ERROR_TM_CANT_MAP								402002
#define ERROR_TM_FAILED_TO_OPEN							402003
#define ERROR_TM_INVALID_PARAM							402004
#define ERROR_TM_TACTION_MISSING_CONNECTED_SEGEMENT		402005
#define ERROR_TM_GENERATECOMMANDBUFFER_BAD_PARAMETER	402006
#define ERROR_TM_TACTIONID_DOESNT_EXIST					402007
#define ERROR_TM_DATABASE_NOT_INITIALIZED				402008
#define ERROR_TM_MAX_CONTROLLER_LIMIT_REACHED			402009
#define ERROR_TM_MAX_ACTION_LIMIT_REACHED				402010
#define ERROR_TM_CONTROLLER_NOT_FOUND					402011
#define ERROR_TM_MAX_TACTORLOCATION_LIMIT_REACHED		402012
#define ERROR_TM_TACTION_NOT_FOUND						402013
#define ERROR_TM_FAILED_TO_UNLOAD						402014
#define ERROR_TM_NO_TACTIONS_IN_DATABASE				402015
#define ERROR_TM_DATABASE_FAILED_TO_OPEN				402016
#define ERROR_TM_FAILED_PACKET_PARSE					402017
#define ERROR_TM_FAILED_TO_CLONE_TACTION				402018

#define EAI_DBM_ERROR									502000
#define EAI_DBM_NO_ERROR								502001

#define ERROR_BAD_DATA 									602000

#endif
