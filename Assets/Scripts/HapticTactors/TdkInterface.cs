using System.Collections;
using System.Runtime.InteropServices;
using System;

namespace Tdk
{
#if UNITY_ANDROID
	public static class TdkInterfacePinvoke
#else
	public static class TdkInterface
#endif // UNITY_ANDROID
	{
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int InitializeTI();

		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int ShutdownTI();

		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr GetVersionNumber();
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int Connect([MarshalAs(UnmanagedType.LPStr)]string name, int type, IntPtr _callback);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern void SetOutgoingDataCallback(IntPtr data_callback);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int Pulse(int deviceId, int tacNum, int duration, int delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern void TactorSelfTest(int deviceId, int delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern void ReadSegmentList(int deviceId, int delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int ReadFW(int deviceId);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern void UpdateTI();
	
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int Close(int deviceId);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int CloseAll();
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int RampGain(int deviceID, int _tacNum, int gainStart,int gainEnd,int duration, int func,int _delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int RampFreq(int deviceID, int _tacNum, int freqStart,int freqEnd,int duration, int func,int _delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int ChangeGain(int deviceID, int _tacNum, int gainVal, int _delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int ChangeFreq(int deviceID, int _tacNum, int freqVal, int _delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int ChangeSigSource(int deviceID, int _tacNum, int type, int _delay);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int Discover(int type);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr GetDiscoveredDeviceName(int index);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int GetDiscoveredDeviceType(int index);

        [DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int BeginStoreTAction(int _deviceID, int tacID);

        [DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int FinishStoreTAction(int _deviceID);

        [DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int PlayStoredTAction(int _deviceID, int _delay, int tacId);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int LoadTActionDatabase([MarshalAs(UnmanagedType.LPStr)] string file);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr GetTActionName(int id);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int GetLoadedTActionSize();
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int GetTActionDuration(int tacID);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int PlayTAction(int boardID, int tacID, int tactorID, float gainScale, float freq1Scale, float freq2Scale, float timeScale);
		
        [DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int PlayTActionToSegment(int boardID, int tacID, int tactorIDOffset, int controllerSegmentID, float gainScale, float freq1Scale, float freq2Scale, float timeScale);

		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int Stop(int deviceID, int _delay);

		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int SetTactors(int deviceID, int _delay, byte[] states);

		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int SetTactorType(int deviceID, int _delay, int tactor, int type);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int WriteToBoard(int deviceID, byte[] data, int data_length);
			
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int GetLastEAIError();

		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern void SetKillOnNaks(bool true_for_kill);
		
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int IsDatabaseLoaded();
		
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int UnloadTActions();
		
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int UnloadTAction(byte[] uuid);
			
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int PlayNonLoadedTAction(int boardID,System.IntPtr serializedData,System.IntPtr segptr,System.IntPtr taActionptr,System.IntPtr dataptr,System.IntPtr tactorsptr);
		
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int SaveNonLoadedTAction(byte[] database,byte[] name,System.IntPtr serializedData,System.IntPtr segptr,System.IntPtr taActionptr,System.IntPtr dataptr,System.IntPtr tactorsptr);
		
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int SaveTAction(byte[] database,byte[] name,byte[] tid);
		
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int GetTAction(byte[] name,byte[] id,System.IntPtr serializedData,System.IntPtr segptr,System.IntPtr taActionptr,System.IntPtr dataptr,System.IntPtr tactorsptr);
		
		[DllImport("TactorInterface", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int RemapTAction(int boardID,byte[] name,byte[] id,System.IntPtr serializedData,System.IntPtr segptr,System.IntPtr taActionptr,System.IntPtr dataptr,System.IntPtr tactorsptr,int TactorID);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int DeleteTActionFromDatabase(byte[] database,byte[] uuid);
		
		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int IsInDatabase(byte[] dbName,byte[]name,byte[] id,System.IntPtr serializedData,System.IntPtr segptr,System.IntPtr taActionptr,System.IntPtr dataptr,System.IntPtr tactorsptr);

		[DllImport("TactorInterface", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern void SetTactorStateCallback(IntPtr ts_callback);
	}

	public static class TdkDefines
	{
		// the version of TDK this is built against.
		public static Version TdkVersion
		{
			get
			{
				IntPtr verstr = TdkInterface.GetVersionNumber();

				if (verstr == IntPtr.Zero)
					return new Version(0,0,0,0);

				string ver = Marshal.PtrToStringAnsi(verstr);
				return new Version(ver);
			}
		}

		// a signature for a data callback
		[System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
		public delegate void DataCallbackDelegate(int id,System.IntPtr bytes,int size);

		// a signature for a tactor state callback
		[System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
		public delegate void TactorStateCallbackDelegate(int id, int duration, int tactors, int delay);

		// pulled from EAI_Defines.h
		public static readonly int DeviceTypeCount = 4;
		[Flags]
		public enum DeviceTypes
		{
			Unknown = 0x00,
			Serial = 0x01,
			WinUsb = 0x02,
			AndroidBluetooth = 0x04,
			AndroidUsb = 0x08
		}
		
		// pulled from EAI_Defines.h
		public static readonly int RampLinear = 0x01;

		// pulled from EAI_Defines.h
		public enum TactorTypes
		{
			C3 = 0x11,
			C2 = 0x12,
			EMS = 0x21,
			EMR = 0x22
		}
		
		// Pulled from EAI_Defines.h
		public enum ErrorCodes
		{
			ERROR_NOINIT = 202000,
			ERROR_CONNECTION = 202001,
			ERROR_BADPARAMETER = 202002,
			ERROR_INTERNALERROR = 202003,
			ERROR_PARTIALREAD = 202004,
			ERROR_HANDLE_NULL = 202005,
			ERROR_WIN_ERROR = 202006,
			ERROR_EAITIMEOUT = 202007,
			ERROR_EAINOREAD = 202008,
			ERROR_FAILED_TO_CLOSE = 202009,
			ERROR_MORE_TO_READ = 202010,
			ERROR_FAILED_TO_READ = 202011,
			ERROR_FAILED_TO_WRITE = 202012,
			ERROR_NO_SUPPORTED_DRIVER = 202013,
			
			ERROR_PARSE_ERROR = 203000,
			
			ERROR_DM_ACTION_LIMIT_REACHED = 204010,
			ERROR_DM_FAILED_TO_GENERATE_DEVICE_ID = 204011,
			
			ERROR_JNI_UNKNOWN = 205000,
			ERROR_JNI_BAD = 205001,
			ERROR_JNI_FIND_CLASS_ERROR = 205002,
			ERROR_JNI_FIND_FIELD_ERROR = 205003,
			ERROR_JNI_FIND_METHOD_ERROR = 205004,
			ERROR_JNI_CALL_METHOD_ERROR = 205005,
			ERROR_JNI_RESOURCE_ACQUISITION_ERROR = 205006,
			ERROR_JNI_RESOURCE_RELEASE_ERROR = 205007,
			
			ERROR_SI_ERROR = 302000,
			
			ERROR_TM_NOT_INITIALIZED = 402000,
			ERROR_TM_NO_DEVICE = 402001,
			ERROR_TM_CANT_MAP = 402002,
			ERROR_TM_FAILED_TO_OPEN = 402003,
			ERROR_TM_INVALID_PARAM = 402004,
			ERROR_TM_TACTION_MISSING_CONNECTED_SEGEMENT = 402005,
			ERROR_TM_GENERATECOMMANDBUFFER_BAD_PARAMETER = 402006,
			ERROR_TM_TACTIONID_DOESNT_EXIST = 402007,
			ERROR_TM_DATABASE_NOT_INITIALIZED = 402008,
			ERROR_TM_MAX_CONTROLLER_LIMIT_REACHED = 402009,
			ERROR_TM_MAX_ACTION_LIMIT_REACHED = 402010,
			ERROR_TM_CONTROLLER_NOT_FOUND = 402011,
			ERROR_TM_MAX_TACTORLOCATION_LIMIT_REACHED = 402012,
			ERROR_TM_TACTION_NOT_FOUND = 402013,
			ERROR_TM_FAILED_TO_UNLOAD = 402014,
			ERROR_TM_NO_TACTIONS_IN_DATABASE = 402015,
			ERROR_TM_DATABASE_FAILED_TO_OPEN = 402016,
			ERROR_TM_FAILED_PACKET_PARSE = 402017,
			ERROR_TM_FAILED_TO_CLONE_TACTION = 402018,
			
			EAI_DBM_ERROR = 502000,
			EAI_DBM_NO_ERROR = 502001,
			
			ERROR_BAD_DATA = 602000
		}
		
		public static string ErrorCodeToString(int error_code)
		{
			if (Enum.IsDefined(typeof(ErrorCodes), error_code))
			{
				return ((ErrorCodes)error_code).ToString();
			}
			
			return ("ERROR_UNDEFINED_ERROR_" + error_code.ToString());
		}
		
		public static string GetLastEAIErrorString()
		{
			return ErrorCodeToString(TdkInterface.GetLastEAIError());
		}
		
		public static string NakReasonToString(byte reason_byte)
		{
			string reason = "??????";
			
			switch (reason_byte)
			{
			case 0x01: //The ETX value was not found in the expected place
				reason = "The ETX value was not found in the expected place";
				break;
			case 0x02: //The Checksum value was invalid
				reason = "The Checksum value was invalid";
				break;
			case 0x03: //Insufficient Data in packet
				reason = "Insufficient Data in packet";
				break;
			case 0x04: //An invalid command was used
				reason = "An invalid command was used";
				break;
			case 0x05: //Invaid Data was given with a valid command
				reason = "Invaid Data was given with a valid command";
				break;
			case 0x06: //Data length exceeds max payload
				reason = "Data length exceeds max payload";
				break;
			case 0x07: //Invalid Sequence Data
				reason = "Invalid Sequence Data";
				break;
			case 0x08: //Bus Error Sending to Node failed after three attempts
				reason = "Bus Error Sending to Node failed after three attempts";
				break;
			case 0x09: //Bus Master Times out while waiting on all bytes of incoming command to finish
				reason = "Bus Master Times out while waiting on all bytes of incoming command to finish";
				break;
			case 0x0A: //Node has a Fault Cannot Complete Request
				reason = "Node has a Fault Cannot Complete Request";
				break;
			case 0x0B: // Sequence is busy. you tried to perform a new sequence operation while the previous one is still in progress ? sequences not implemented on TDK boards
				reason = "Sequence is busy";
				break;
			case 0x0C:  // not used anymore
				reason = "Timeout";
				break;
			case 0x0D:   // Ramp List is Full
				reason = "Ramp List is Full";
				break;
			case 0x0E:   // Action List is Full
				reason = "Action List is Full";
				break;
			case 0x0F: // Internal Error
				reason = "Internal Error";
				break;
			case 0x10:// Master Packet is Full - you shouldn?t see this one ? it means the Bus on eTSAS or other new distrib system is very busy with action-lists & ramps and can?t fit a new command at the instant you sent it.
				reason = "Master Packet is Full";
				break;
			case 0x11: // error while processing a command which is already in the action list
				reason = "Action List Has Too Many Bytes";
				break;
			case 0x12:  // another error while processing a command which is already in the action list
				reason = "Action List Has Too Few Bytes";
				break;
			case 0x13:  //your packet is too big to fit in the TDK receive buffer
				reason = "Packet too Large to Fit In Receieve Buffer";
				break;
			case 0x14:  //Received a command while Self Test is running
				reason = "Received a command while Self Test is running";
				break;
			default:
				reason = "UNKNOWN ERROR";
				break;
			}
			
			return reason;
		}
	}

}
