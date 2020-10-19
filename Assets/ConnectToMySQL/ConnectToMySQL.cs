using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Text;
using System.Linq;
using UnityEngine.UI;

public enum LogUploadStatus {
    Success,
    Error,
}

public class LogUploadResult {
	public LogUploadStatus status;
	public string error;
}

[System.Serializable]
public class TargetCredentials {
	public string label;
	public string dbName;
	public string table;
	public string username;
	public string password;
	public string dbURL;
	public string dbSecKey;

	public static TargetCredentials CreateFromJSON(string jsonString) {
		return JsonUtility.FromJson<TargetCredentials>(jsonString);
	}

	public string SaveToString() {
		return JsonUtility.ToJson(this);
	}
}

[System.Serializable]
public class DumpedForm {

	public string dbName;
	public string table;
	public string username;
	public string password;
	public string dbURL;
	public string dbSecKey;

	public string dbCols;
	public string dataString;

	public static DumpedForm CreateFromJSON(string jsonString) {
		return JsonUtility.FromJson<DumpedForm>(jsonString);
	}

	public string SaveToString() {
		return JsonUtility.ToJson(this);
	}

}

public class DataTarget {
	public TargetCredentials credentials = new TargetCredentials();
	public List<Dictionary<string, Dictionary<int, object>>> logsToUpload = new List<Dictionary<string, Dictionary<int, object>>>();
}

public class ConnectToMySQL : MonoBehaviour {
	private Dictionary<string, DataTarget> dataTargets = new Dictionary<string, DataTarget>();
	private List<DumpedForm> dumpedforms = new List<DumpedForm>();

	public static string response = "";
	public static bool dataReceived = false;
	public static ConnectToMySQL instance = null;

	private static bool isConnected = false;
	private int retries = 0;
	private string secHash;
    private string dataHash;
	private string colsHash;

	private StreamWriter writer;
	private string directory;
	private string fileName;
	private string sep = ",";

	[Header("(Optional) Deployment Settings")]
	[Tooltip("Input a mysql_auth.txt here to remove the mySQL UI dialog on startup.")]
	[SerializeField]
	private TextAsset[] builtInCredentials;
	//private Dictionary<string, string> credentials;
	//private Dictionary<string, string> tables;

	private float timeout = 2;
	private float lastUploadTime = -1f;
	private bool dumplock = false;

	[Header("(Optional) UI Settings")]
	[Tooltip("Use this to indicate server status through a UI text field.")]
	[SerializeField]
	private Text statusMessage;

	[Tooltip("Set the color of the UI text field when an error is encountered.")]
	[SerializeField]
	private Color errorColor;
	private Color defaultColor;

	private GameObject mysqlCred;
	private InputField labelInputField;
	private InputField dbSecKeyInputField;
	private InputField dbNameField;
	private InputField tableNameField;
	private InputField userNameField;
	private InputField passwordField;
	private InputField dbURLField;
	private GameObject connectButton;

	private GameObject eventSystem;

	[Serializable]
    public class OnLogsUploaded : UnityEvent<LogUploadResult> { }
    public OnLogsUploaded onLogsUploaded;

	void Awake() {
		if (statusMessage != null) {
			defaultColor = statusMessage.color;
		}
		//credentials = new Dictionary<string, string>();
		//logsToUpload = new List<Dictionary<string, List<string>>>();

		directory = Application.persistentDataPath + "/Data/";
		string authDirectory = Application.persistentDataPath + "/Auth/";

		if(!Directory.Exists(directory)) {
			Directory.CreateDirectory(directory);
		}

		if(!Directory.Exists(authDirectory)) {
			Directory.CreateDirectory(authDirectory);
		}
		if (builtInCredentials == null || builtInCredentials.Length == 0)  {
			if (Application.platform != RuntimePlatform.WebGLPlayer) {
				DetectCredentialsOnDisk();
			}
		} else {
			LoadBuiltInCredentials();
		}

		if (instance == null) {
			instance = this;
			if (!isConnected && dataTargets.Keys.Count > 0) {
				TestConnectionToServer();
			}
		}
		if (Application.platform != RuntimePlatform.WebGLPlayer) {
			DetectDumpedLogs ();
		}
	}

	public void TestConnectionToServer () {
				WWWForm testForm = new WWWForm ();
				foreach (KeyValuePair<string, DataTarget> pair in dataTargets) {
					TargetCredentials creds = pair.Value.credentials;
					secHash = Md5Sum (creds.dbSecKey);
					testForm.AddField ("secHashPost", "connectiontest");
					StartCoroutine(ConnectToServer (testForm, creds.dbURL));
					break;
				}
	}

	IEnumerator ConnectToServer(WWWForm form, string dbURL) {

		if (dbURL != "") {
			UnityWebRequest www = UnityWebRequest.Post(dbURL, form);

			yield return www.SendWebRequest();

			if(www.isNetworkError || www.isHttpError) {
				Debug.LogError(www.error);
				Debug.LogError(www.downloadHandler.text);				
				yield return new WaitForSeconds(2.0f);
				retries++;
				if (retries < 3) {
					StartCoroutine (ConnectToServer (form, dbURL));
					if (statusMessage != null) {
						statusMessage.text = "Establishing Connection..";
						statusMessage.color = defaultColor;
					}
				} else {
					if (statusMessage != null) {
						statusMessage.text = "DB Error: Could not reach the database.";
						statusMessage.color = errorColor;
					}
					connectButton.SetActive(true);
				}
			} else {
				print ("Connected to Server");
				if (statusMessage != null) {
					statusMessage.text = "Connected to Server";
					statusMessage.color = defaultColor;
				}
				isConnected = true;
				if (Application.platform != RuntimePlatform.WebGLPlayer) {
					SaveCredentialsToDisk();
				}
				Destroy(mysqlCred);
				if (eventSystem != null) {
					Destroy(eventSystem);
				}
			}
		} else {
			Debug.LogError("No URL was assigned");
			connectButton.SetActive(true);
		}
	}

	public void AddToUploadQueue(Dictionary<string, Dictionary<int, object>> logCollection, string targetLabel) {
		
		if (logCollection.Keys.Count == 0) {
			Debug.LogError("logCollection contain no columns!");
			return;
		}

		if (!logCollection.ContainsKey("Email")) {
			Debug.LogError("Log Collection does not contain a column with AAU Email Addresses!");
			return;
		}

		foreach (string key in logCollection.Keys) {
			if (logCollection[key] == null) {
				Debug.LogError("LogCollection contains column with no initialized Dictionary<int, object> for column " + key  + ". Please ensure that columns have data before adding to the upload queue.");
				Debug.LogError("Aborting AddToUploadQueue..");
				return;
			}

			if (logCollection[key].Keys.Count == 0) {
				Debug.LogError("Dictionary Present, but no data present in column " + key  + ". Please ensure that columns have data before adding to the upload queue.");
				Debug.LogError("Aborting AddToUploadQueue..");
				return;
			}

			if (logCollection[key].Keys.Count != logCollection["Email"].Keys.Count)  {
				Debug.LogWarning("The " + key + " column contain " + logCollection[key].Keys.Count + " rows, but the e-mail column contains " + logCollection["Email"].Keys.Count + "! Empty values will be passed on as NULL values.");
			}
		}
		if (dataTargets.ContainsKey(targetLabel)) {
			dataTargets[targetLabel].logsToUpload.Add(new Dictionary<string, Dictionary<int, object>>(logCollection));
			Debug.Log ("Log Collection " + targetLabel + " with " + logCollection.Count + " columns and " + logCollection["Email"].Keys.Count + " rows prepared for upload.");
		} else {
			Debug.LogError("The targetLabel " + targetLabel + " does not match any of the loaded targets.");
			Debug.LogError("Aborting AddToUploadQueue..");
		}
	}

	public void UploadNow() {

		//if (Time.time - lastUploadTime < timeout) {
		//	Debug.LogWarning("Don't make frequent calls to MySQL.UploadNow(), uploading is an async operation!");
		//}

		lastUploadTime = Time.time;

		if (dataTargets.Keys.Count == 0) {
			Debug.LogError("No logs in the upload queue. Use AddToUploadQueue() to add a logCollection to the queue before calling UploadNow().");
			return;
		}
		if (dataTargets.Keys.Count > 0) {
			foreach (KeyValuePair<string, DataTarget> pair in dataTargets) {
				DataTarget target = pair.Value;
				foreach (Dictionary<string, Dictionary<int, object>> logCollection in target.logsToUpload) {
					Debug.Log ("Attempting to upload logCollection with " + logCollection.Count + " rows.");
					string dbCols = string.Join(sep,logCollection.Keys.ToArray());
					string dataString = ParseDataToString(logCollection);
					WWWForm form = PrepareForm(dbCols, dataString, target.credentials);

					// Store data for later in case we need to dump them.
					DumpedForm df = new DumpedForm();
					df.dbName = target.credentials.dbName;
					df.table = target.credentials.table;
					df.username = target.credentials.username;
					df.password = target.credentials.password;
					df.dbURL = target.credentials.dbURL;
					df.dbSecKey = target.credentials.dbSecKey;

					df.dbCols = dbCols;
					df.dataString = dataString;
					dumpedforms.Add(df);

					StartCoroutine (SubmitLogs (form, target.credentials.dbURL));
				}
				target.logsToUpload.Clear();
			}
		}
	}

    // Converts the values of the parameters (in a "object format") to a string, formatting them to the
    // correct format in the process.
    private string ConvertToString(object arg, string col)
    {
		string processedArg;
        if (arg is float)
        {
            processedArg = ((float)arg).ToString("0.0000").Replace(",", ".");
        } else if (arg is int) {
            processedArg = arg.ToString();
        } else if (arg is bool) {
            processedArg = ((bool)arg) ? "TRUE" : "FALSE";
        } else if (arg is Vector3)
        {
            processedArg = ((Vector3)arg).ToString("0.0000").Replace(",", ".");
        }
        else
        {
            processedArg = arg.ToString();
        }

		if (processedArg.Contains(",")) {
			Debug.LogWarning("Value " + processedArg + "from column " + col + "contains comma (,). It has been replaced with a dot.");
			 processedArg.Replace(',', '.');
		} else if (processedArg.Contains(";")) {
			Debug.LogWarning("Value " + processedArg + "from column " + col + "contains semi-colon (;). It has been replaced with a dash.");
			processedArg.Replace(';', '-');
		} else if (processedArg.Contains("\"")) {
			Debug.LogWarning("Value " + processedArg + "from column " + col + "contains quotation mark (\"). It has been replaced with a dash.");
			processedArg.Replace('\"', '-');
		} else if (processedArg.Contains("\n")) {
			Debug.LogWarning("Value " + processedArg + "from column " + col + "contains return character. It has been removed.");
			processedArg.Replace("\n", String.Empty);
		} else if (processedArg.Contains("\r")) {
			Debug.LogWarning("Value " + processedArg + "from column " + col + "contains return character. It has been removed.");
			processedArg.Replace("\r", String.Empty);
		}
		return processedArg;
    }

	private string ParseDataToString(Dictionary<string, Dictionary<int, object>> logCollection) {
		// Create a string with the data
		string dataString = "";
		object temp;
		for(int i = 0; i < logCollection["Email"].Keys.Count; i++) {
			List<string> row = new List<string>();
			foreach(string key in logCollection.Keys) {
				if (logCollection[key].TryGetValue(i, out temp)) {
					row.Add(ConvertToString(temp, key));
				} else {
					row.Add("NULL");
				}
			}
			if(i != 0) {
				dataString += ";";
			}
			dataString += string.Join(sep,row.ToArray());
		}
		return dataString;
	}

	private WWWForm PrepareForm(string dbCols, string dataString, TargetCredentials credentials) {
		WWWForm form = new WWWForm ();

		// Add credentials to form
		form.AddField ("dbnamePost", credentials.dbName);
		form.AddField ("tablePost", credentials.table);
		form.AddField ("usernamePost", credentials.username);
		form.AddField ("passwordPost", credentials.password);
		form.AddField ("secHashPost",Md5Sum (credentials.dbSecKey));

        colsHash = Md5Sum(dbCols);
		form.AddField("db_hash", colsHash);
		Debug.Log ("columns to insert: " + dbCols);
		form.AddField ("dbCol", dbCols);

        dataHash = Md5Sum(dataString);
        form.AddField("dataHashPost", dataHash);
		Debug.Log ("data to submit: " + dataString);
		form.AddField ("dataPost", dataString);
		form.AddField("dbURL", credentials.dbURL);
		return form;
	}

	private IEnumerator SubmitLogs(WWWForm form, string dbURL) {
		Debug.Log ("Submitting logs..");
		if (statusMessage != null) {
			statusMessage.text = "Submitting logs..";
			statusMessage.color = defaultColor;
		}
		UnityWebRequest www = UnityWebRequest.Post(dbURL, form);

		yield return www.SendWebRequest();

		LogUploadResult logUploadResult = new LogUploadResult();
		if(www.isNetworkError || www.isHttpError) {
			logUploadResult.status = LogUploadStatus.Error;
			logUploadResult.error = www.error;
			onLogsUploaded.Invoke(logUploadResult);
            Debug.LogError(("Unable to submit logs: " + www.error));
			if (statusMessage != null) {
				statusMessage.text = (www.downloadHandler.text);
				statusMessage.color = errorColor;
			}
            Debug.LogError(www.downloadHandler.text);
			while (dumplock) {
				yield return new WaitForSeconds(1f);
			}
			dumplock = true;
			if (Application.platform != RuntimePlatform.WebGLPlayer) {
				DumpLogsToUpload();				
			}
			dumplock = false;	
        } else {
			Debug.Log ("Posted successfully");
			if (statusMessage != null) {
				statusMessage.text = "Posted successfully";
				statusMessage.color = defaultColor;			
			}
			logUploadResult.status = LogUploadStatus.Success;
			logUploadResult.error = "";
			onLogsUploaded.Invoke(logUploadResult);
		}

	}

	private void DumpLogsToUpload() {
		for (int i = 0; i < dumpedforms.Count; i++) {
			if (dumpedforms[i] != null) {
				var fileDumps = Directory.GetFiles(directory, "logdump*");
				
				using (StreamWriter writer = File.AppendText (directory + "logdump" + fileDumps.Length + ".json")) {
					writer.WriteLine (dumpedforms[i].SaveToString());
				}
				
				Debug.Log ("Dumping logdump to disk at: " + directory);
				dumpedforms[i] = null;
			}
		}
	}

	private void DetectDumpedLogs() {
		// If no credentials are available, we skip dumplog detection.
		if (dataTargets.Keys.Count == 0) {
			Debug.LogWarning("No credentials loaded, aborting logdump detection..");
			return;
		}

		var fileDumps = Directory.GetFiles(directory, "logdump*");

		if (fileDumps == null) {
			return;
		}

		List<DumpedForm> forms = new List<DumpedForm>();

		foreach (string filename in fileDumps) {
			// determine whether  file is empty.
			var fi = new FileInfo(filename);
			if (fi.Length == 0) {
				Debug.LogWarning("empty logdump " + filename + ", deleting coldump and logdump..");
				File.Delete (filename);
				continue;
			}

			string line;
			string json = "";

			DumpedForm form = new DumpedForm();

			using (StreamReader reader = new StreamReader(filename)) {
				while((line = reader.ReadLine()) != null)  
				{  
					json += line;
				}
			}

			JsonUtility.FromJsonOverwrite(json, form);
			

			File.Delete (filename);

			// determine whether filestrings are empty.
			if (String.IsNullOrEmpty(json.Trim())) {
				Debug.LogWarning("malformed dump " + filename + ", deleting coldump and logdump..");
				continue;
			}

			Debug.Log ("Dumped Logs Detected.");
			forms.Add(form);
		}

		foreach (DumpedForm dumpform in forms) {
			TargetCredentials creds = new TargetCredentials() {
				dbName = dumpform.dbName,
				table = dumpform.table,
				username = dumpform.username,
				password = dumpform.password,
				dbURL = dumpform.dbURL,
				dbSecKey = dumpform.dbSecKey
			};
			var form = PrepareForm(dumpform.dbCols, dumpform.dataString, creds);
			StartCoroutine (SubmitLogs (form, dumpform.dbURL));
		}
		
	}

	private static string Md5Sum(string strToEncrypt) {
		System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
		byte[] bytes = ue.GetBytes(strToEncrypt);

		// encrypt bytes
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytes);

		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";

		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}

		return hashString.PadLeft(32, '0');
	}

	public void Toggle_AuthConnect() {
		DataTarget target = new DataTarget();
		target.credentials.label = labelInputField.text;
		target.credentials.dbSecKey = dbSecKeyInputField.text;
		target.credentials.dbName = dbNameField.text;
		target.credentials.table = tableNameField.text;
		target.credentials.username = userNameField.text;
		target.credentials.password = passwordField.text;
		target.credentials.dbURL = dbURLField.text;
		dataTargets.Add(target.credentials.table, target);
		TestConnectionToServer();
	}


	private void SaveCredentialsToDisk() {
		foreach (KeyValuePair<string, DataTarget> pair in dataTargets) {
			DataTarget target = pair.Value;
			string authDirectory = Application.persistentDataPath + "/Auth/";

			if (File.Exists (authDirectory + "mysql_" + target.credentials.label + ".txt")) {
				File.Delete (authDirectory + "mysql_" + target.credentials.label + ".txt");
			}

			string json = target.credentials.SaveToString();

			using (StreamWriter writer = File.AppendText (authDirectory + "mysql_" + target.credentials.label + ".json")) {
				writer.WriteLine (json);
			}

			Debug.Log("Credentials saved to: " + authDirectory + "mysql_" + target.credentials.label + ".json");
		}
	}

	private void LoadBuiltInCredentials() {

		foreach (TextAsset creds in builtInCredentials) {
			DataTarget target = new DataTarget();
			target.credentials = TargetCredentials.CreateFromJSON(creds.text);
			Debug.Log(target.credentials.label);
			dataTargets.Add(target.credentials.label, target);
		}
	}

	private void DetectCredentialsOnDisk() {
		string authDirectory = Application.persistentDataPath + "/Auth/";

		var credentialFiles = Directory.GetFiles(authDirectory, "mysql*");
		Debug.Log(credentialFiles.Length);
		if (credentialFiles.Length > 0) {
			foreach  (var filepath in credentialFiles) {
			Debug.Log("Loading credentials from: " + filepath);
			string line;
			string json = "";
			using (StreamReader reader = new StreamReader(filepath)) {
				while((line = reader.ReadLine()) != null)  
				{  
					json += line;
				}
			}

			DataTarget target = new DataTarget();
			target.credentials = TargetCredentials.CreateFromJSON(json);
			dataTargets.Add(target.credentials.label, target);
			}
		} else {
			mysqlCred = Instantiate(Resources.Load("MySQLAuthCanvas", typeof(GameObject))) as GameObject;
			mysqlCred.SetActive(true);

			if (UnityEngine.EventSystems.EventSystem.current == null) {
				eventSystem = Instantiate(Resources.Load("MySQLEventSystem", typeof(GameObject))) as GameObject;
				eventSystem.SetActive(true);
			}

			var inputFields = mysqlCred.GetComponentsInChildren<InputField>();
			labelInputField = inputFields[0];
			dbSecKeyInputField = inputFields[1];
			dbNameField = inputFields[2];
			tableNameField = inputFields[3];
			userNameField = inputFields[4];
			passwordField = inputFields[5];
			dbURLField = inputFields[6];

			connectButton = GameObject.Find("MySQLConnectButton");
			Button button = connectButton.GetComponent<Button>();
			button.onClick.AddListener(() => Toggle_AuthConnect());
		}
		
	}
		
}
