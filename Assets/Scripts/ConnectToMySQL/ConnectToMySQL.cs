using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using UnityEngine.UI;

public class ConnectToMySQL : MonoBehaviour {
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
	private List<Dictionary<string, List<string>>> logsToUpload;
	private string sep = ",";
	private List<string> dataDumps;
	private List<string> colDumps;

	[Header("(Optional) Deployment Settings")]
	[Tooltip("Input a mysql_auth.txt here to remove the mySQL UI dialog on startup.")]
	[SerializeField]
	private TextAsset builtInCredentials;
	private Dictionary<string, string> credentials;

	private float timeout = 5;
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
	private InputField emailInputField;
	private InputField dbSecKeyInputField;
	private InputField dbNameField;
	private InputField tableNameField;
	private InputField userNameField;
	private InputField passwordField;
	private InputField dbURLField;
	private GameObject connectButton;

	private GameObject eventSystem;
	void Awake() {
		if (statusMessage != null) {
			defaultColor = statusMessage.color;
		}
		dataDumps = new List<string>();
		colDumps = new List<string>();
		credentials = new Dictionary<string, string>();
		logsToUpload = new List<Dictionary<string, List<string>>>();

		directory = Application.persistentDataPath + "/Data/";
		string authDirectory = Application.persistentDataPath + "/Auth/";

		if(!Directory.Exists(directory)) {
			Directory.CreateDirectory(directory);
		}

		if(!Directory.Exists(authDirectory)) {
			Directory.CreateDirectory(authDirectory);
		}

		if (builtInCredentials == null)  {
			DetectCredentialsOnDisk();
		} else {
			LoadBuiltInCredentials();
		}

		if (instance == null) {
			instance = this;
			if (!isConnected && credentials.Keys.Count > 0) {
				TestConnectionToServer();
			}
		}

		DetectDumpedLogs ();
	}

	public void TestConnectionToServer () {
				WWWForm testForm = new WWWForm ();
				secHash = Md5Sum (credentials["dbSecKey"]);
				testForm.AddField ("secHashPost", "connectiontest");
				StartCoroutine(ConnectToServer (testForm));
	}

	IEnumerator ConnectToServer(WWWForm form) {

		if (credentials["dbURL"] != "") {
			UnityWebRequest www = UnityWebRequest.Post(credentials["dbURL"], form);

			yield return www.SendWebRequest();

			if(www.isNetworkError || www.isHttpError) {
				Debug.LogError(www.error);
				Debug.LogError(www.downloadHandler.text);				
				yield return new WaitForSeconds(2.0f);
				retries++;
				if (retries < 3) {
					StartCoroutine (ConnectToServer (form));
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
				SaveCredentialsToDisk();
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

	public void AddToUploadQueue(Dictionary<string, List<string>> logCollection) {
		
		if (logCollection.Count == 0) {
			Debug.LogError("logCollection was not initialized with any List<string> columns!");
			return;
		}

		if (!logCollection.ContainsKey("Email")) {
			Debug.LogError("Log Collection does not contain a column with AAU Email Addresses!");
			return;
		}

		foreach (string key in logCollection.Keys) {
			if (logCollection[key].Count == 0) {
				Debug.LogError("No Data Present in column " + key  + ". Please ensure that columns have data before adding to the upload queue.");
				Debug.LogError("Aborting AddToUploadQueue..");
				return;
			}

			if (logCollection[key].Count != logCollection["Email"].Count)  {
				Debug.LogError("The " + key + " column contain more data than the e-mail column! Please make sure all columns are equal length.");
				Debug.LogError("Aborting AddToUploadQueue..");
				return;
			}
		}

		logsToUpload.Add(new Dictionary<string, List<string>>(logCollection));

		Debug.Log ("Log Collection with " + logCollection.Count + " columns prepared for upload.");
	}

	public void UploadNow() {

		if (Time.time - lastUploadTime < timeout) {
			Debug.LogWarning("Don't make frequent calls to MySQL.UploadNow(), uploading is an async operation!");
		}

		lastUploadTime = Time.time;

		if (logsToUpload.Count == 0) {
			Debug.LogError("No logs in the upload queue. Use AddToUploadQueue() to add a logCollection to the queue before calling UploadNow().");
			return;
		}
		if (logsToUpload.Count > 0) {
			foreach (Dictionary<string, List<string>> logCollection in logsToUpload) {
				Debug.Log ("Attempting to upload logCollection with " + logCollection.Count + " rows.");
				string dbCols = string.Join(sep,logCollection.Keys.ToArray());
				string dataString = ParseDataToString(logCollection);
				WWWForm form = PrepareForm(dbCols, dataString);

				// dump the data in case submission fails
				dataDumps.Add(dataString);
				colDumps.Add(dbCols);

				StartCoroutine (SubmitLogs (form));
			}
			logsToUpload.Clear();
		}
	}
	private string ParseDataToString(Dictionary<string, List<string>> logCollection) {
		// Create a string with the data
		string dataString = "";
		for(int i = 0; i < logCollection["Email"].Count; i++) {
			List<string> row = new List<string>();
			foreach(string key in logCollection.Keys) {
				if (logCollection[key][i].Contains(",")) {
					Debug.LogWarning("Value " + logCollection[key] + "from column " + key + "contains comma (,). It has been replaced with a dot.");
					logCollection[key][i].Replace(',', '.');
				} else if (logCollection[key][i].Contains(";")) {
					Debug.LogWarning("Value " + logCollection[key] + "from column " + key + "contains semi-colon (;). It has been replaced with a dash.");
					logCollection[key][i].Replace(';', '-');
				} else if (logCollection[key][i].Contains("\"")) {
					Debug.LogWarning("Value " + logCollection[key] + "from column " + key + "contains quotation mark (\"). It has been replaced with a dash.");
					logCollection[key][i].Replace('\"', '-');
				}
				row.Add(logCollection[key][i]);
			}
			if(i != 0) {
				dataString += ";";
			}
			dataString += string.Join(sep,row.ToArray());
		}
		return dataString;
	}

	private WWWForm PrepareForm(string dbCols, string dataString) {
		WWWForm form = new WWWForm ();

		// Add credentials to form
		form.AddField ("dbnamePost", credentials["dbName"]);
		form.AddField ("tablePost", credentials["tableName"]);
		form.AddField ("usernamePost", credentials["username"]);
		form.AddField ("passwordPost", credentials["password"]);
		form.AddField ("secHashPost",Md5Sum (credentials["dbSecKey"]));

        colsHash = Md5Sum(dbCols);
		form.AddField("db_hash", colsHash);
		Debug.Log ("columns to insert: " + dbCols);
		form.AddField ("dbCol", dbCols);

        dataHash = Md5Sum(dataString);
        form.AddField("dataHashPost", dataHash);
		Debug.Log ("data to submit: " + dataString);
		form.AddField ("dataPost", dataString);
		return form;
	}

	private IEnumerator SubmitLogs(WWWForm form) {
		
		Debug.Log ("Submitting logs..");
		if (statusMessage != null) {
			statusMessage.text = "Submitting logs..";
			statusMessage.color = defaultColor;
		}
		UnityWebRequest www = UnityWebRequest.Post(credentials["dbURL"], form);

		yield return www.SendWebRequest();

		if(www.isNetworkError || www.isHttpError) {
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
			DumpLogsToUpload();				
			dumplock = false;
        } else {
			Debug.Log ("Posted successfully");
			if (statusMessage != null) {
				statusMessage.text = "Posted successfully";
				statusMessage.color = defaultColor;			
			}
			// Clear datadump structures in case we are submitting dumped data
			dataDumps.Clear();
			colDumps.Clear();
		}

	}

	private void DumpLogsToUpload() {
		if (dataDumps.Count > 0 && colDumps.Count > 0) {
			for (int i = 0; i < dataDumps.Count; i++) {
				var fileDumps = Directory.GetFiles(directory, "logdump*");
				using (StreamWriter writer = File.AppendText (directory + "logdump" + fileDumps.Length)) {
					writer.WriteLine (dataDumps[i]);
				}

				// If a coldump file already exists, we overwrite it.
				if (File.Exists (directory + "coldump" + fileDumps.Length)) {
					Debug.LogWarning("Overwriting coldump..");
					File.Delete (directory + "coldump" + fileDumps.Length);
				}

				using (StreamWriter writer = File.AppendText (directory + "coldump" + fileDumps.Length)) {
					writer.WriteLine (colDumps[i]);
				}
			}
			dataDumps.Clear();
			colDumps.Clear();
			Debug.Log ("Dumping logdump to disk at: " + directory);
		}	
	}

	private void DetectDumpedLogs() {
		// If no credentials are available, we skip dumplog detection.
		if (credentials == null) {
			Debug.LogWarning("No credentials loaded, aborting logdump detection..");
			return;
		}

		var fileDumps = Directory.GetFiles(directory, "logdump*");

		if (fileDumps == null) {
			return;
		}

		for (int i = 0; i < fileDumps.Length; i++) {
			// Remove logdump if we don't have a corresponding coldump file.
			if (!File.Exists (directory + "coldump" + i)) {
				Debug.LogWarning("logdump " + i + " was malformed, deleting logdump..");
				File.Delete (directory + "logdump" + i);
				continue;
			}

			// Remove coldump if no logdump exists
			if (!File.Exists(directory + "logdump" + i)) {
				Debug.LogWarning("no logdump " + i + " available, deleting coldump..");
				File.Delete (directory + "coldump" + i);
				continue;
			}

			// determine whether  file is empty.
			var fi = new FileInfo(directory + "logdump" + i);
			if (fi.Length == 0) {
				Debug.LogWarning("empty logdump " + i + ", deleting coldump and logdump..");
				File.Delete (directory + "coldump" + i);
				File.Delete (directory + "logdump" + i);
				continue;
			}

			string line;

			string dataString = "";

			using (StreamReader reader = new StreamReader(directory + "logdump" + i)) {
				while((line = reader.ReadLine()) != null)  
				{  
					dataString += line;
				}
			}

			string dbCols = "";

			using (StreamReader reader = new StreamReader(directory + "coldump" + i)) {
				while((line = reader.ReadLine()) != null)  
				{  
					dbCols += line;
				}
			}

			File.Delete (directory + "logdump" + i);
			File.Delete (directory + "coldump" + i);

			// determine whether filestrings are empty.
			if (String.IsNullOrEmpty(dataString.Trim()) || String.IsNullOrEmpty(dbCols.Trim())) {
				Debug.LogWarning("malformed dump " + i + ", deleting coldump and logdump..");
				continue;
			}

			Debug.Log ("Dumped Logs Detected.");
			
			// dump the data in case submission fails
			dataDumps.Add(dataString);
			colDumps.Add(dbCols);

			WWWForm form = PrepareForm(dbCols, dataString);
			StartCoroutine (SubmitLogs (form));
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
		credentials["email"] = emailInputField.text;
		credentials["dbSecKey"] = dbSecKeyInputField.text;
		credentials["dbName"] = dbNameField.text;
		credentials["tableName"] = tableNameField.text;
		credentials["username"] = userNameField.text;
		credentials["password"] = passwordField.text;
		credentials["dbURL"] = dbURLField.text;

		TestConnectionToServer();
	}


	private void SaveCredentialsToDisk() {
		string authDirectory = Application.persistentDataPath + "/Auth/";



		if (File.Exists (authDirectory + "mysql_auth.txt")) {
			File.Delete (authDirectory + "mysql_auth.txt");
		}

		foreach(KeyValuePair<string,string> cred in credentials) {
			using (StreamWriter writer = File.AppendText (authDirectory + "mysql_auth.txt")) {
				writer.WriteLine (cred.Key + "=" + cred.Value);
			}
		}

		Debug.Log("Credentials saved to: " + authDirectory + "mysql_auth.txt");
	}

	private void LoadBuiltInCredentials() {
		string[] cred;
		string[] lines = builtInCredentials.text.Split('\n');
		foreach (var line in lines) {
			if (!string.IsNullOrEmpty(line)) {
				cred = line.Trim().Split('=');
				credentials[cred[0]] = cred[1];
			}
		}
	}

	private void DetectCredentialsOnDisk() {
		string authDirectory = Application.persistentDataPath + "/Auth/";

		if (File.Exists (authDirectory + "mysql_auth.txt")) {
			Debug.Log("Loading credentials from: " + authDirectory + "mysql_auth.txt");
			string line;
			string[] cred;
			using (StreamReader reader = new StreamReader(authDirectory + "mysql_auth.txt")) {
				while((line = reader.ReadLine()) != null)  
				{  
					if (!string.IsNullOrEmpty(line)) {
						cred = line.Split('=');
						credentials[cred[0]] = cred[1];
					}
				}
			}
		} else {
			mysqlCred = Instantiate(Resources.Load("MySQLAuthCanvas", typeof(GameObject))) as GameObject;
			mysqlCred.SetActive(true);

			if (UnityEngine.EventSystems.EventSystem.current == null) {
				eventSystem = Instantiate(Resources.Load("MySQLEventSystem", typeof(GameObject))) as GameObject;
				eventSystem.SetActive(true);
			}

			var inputFields = mysqlCred.GetComponentsInChildren<InputField>();
			emailInputField = inputFields[0];
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
