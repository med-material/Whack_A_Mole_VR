#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class OpenXR_Installer : MonoBehaviour
{
	[MenuItem("VIVE/OpenXR Installer/Install or Update latest version")]
	private static void AddGITPackages()
	{
		AddPackage("https://github.com/ViveSoftware/VIVE-OpenXR.git?path=com.htc.upm.vive.openxr");
	}

	public InputField betInput;
	public static string inputString = "";

	[MenuItem("VIVE/OpenXR Installer/Install specific version")]
	private static void AddGITPackagesByVersion()
	{
		var version = EditorInputDialog.Show("Question", "Please enter the version", "");
		if (!string.IsNullOrEmpty(version))
		{
			Debug.Log("version " + version);

			AddPackage("https://github.com/ViveSoftware/VIVE-OpenXR.git?path=com.htc.upm.vive.openxr#versions/" + version);
		}
	}

	public class EditorInputDialog : EditorWindow
	{
		string description, inputText;
		string okButton, cancelButton;
		bool initializedPosition = false;
		Action onOKButton;

		bool shouldClose = false;

		#region OnGUI()
		void OnGUI()
		{
			// Check if Esc/Return have been pressed
			var e = Event.current;
			if (e.type == EventType.KeyDown)
			{
				switch (e.keyCode)
				{
					// Escape pressed
					case KeyCode.Escape:
						shouldClose = true;
						break;

					// Enter pressed
					case KeyCode.Return:
					case KeyCode.KeypadEnter:
						onOKButton?.Invoke();
						shouldClose = true;
						break;
				}
			}

			if (shouldClose)
			{  // Close this dialog
				Close();
				//return;
			}

			// Draw our control
			var rect = EditorGUILayout.BeginVertical();

			EditorGUILayout.Space(12);
			EditorGUILayout.LabelField(description);

			EditorGUILayout.Space(8);
			GUI.SetNextControlName("inText");
			inputText = EditorGUILayout.TextField("", inputText);
			GUI.FocusControl("inText");   // Focus text field
			EditorGUILayout.Space(12);

			// Draw OK / Cancel buttons
			var r = EditorGUILayout.GetControlRect();
			r.width /= 2;
			if (GUI.Button(r, okButton))
			{
				onOKButton?.Invoke();
				shouldClose = true;
			}

			r.x += r.width;
			if (GUI.Button(r, cancelButton))
			{
				inputText = null;   // Cancel - delete inputText
				shouldClose = true;
			}

			EditorGUILayout.Space(8);
			EditorGUILayout.EndVertical();

			// Force change size of the window
			if (rect.width != 0 && minSize != rect.size)
			{
				minSize = maxSize = rect.size;
			}

			// Set dialog position next to mouse position
			if (!initializedPosition)
			{
				var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
				position = new Rect(mousePos.x + 32, mousePos.y, position.width, position.height);
				initializedPosition = true;
			}
		}
		#endregion OnGUI()

		#region Show()
		/// <summary>
		/// Returns text player entered, or null if player cancelled the dialog.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <param name="inputText"></param>
		/// <param name="okButton"></param>
		/// <param name="cancelButton"></param>
		/// <returns></returns>
		public static string Show(string title, string description, string inputText, string okButton = "OK", string cancelButton = "Cancel")
		{
			string ret = null;
			//var window = EditorWindow.GetWindow<InputDialog>();
			var window = CreateInstance<EditorInputDialog>();
			window.titleContent = new GUIContent(title);
			window.description = description;
			window.inputText = inputText;
			window.okButton = okButton;
			window.cancelButton = cancelButton;
			window.onOKButton += () => ret = window.inputText;
			window.ShowModal();

			return ret;
		}
		#endregion Show()
	}

	static bool AddPackage(string packageFolder)
	{
		Debug.Log("Processing package folder: " + packageFolder);

			var addRequest = Client.Add(packageFolder);
			while (!addRequest.IsCompleted)
			{
				Thread.Sleep(100);
			}
			try
			{
				Debug.Log("Has Error?" + addRequest.Error == null ? "No" : addRequest.Error.message);
				Debug.Log("Has Status?" + addRequest.Status);
				Debug.Log("Has Result?" + addRequest.Result);
				return addRequest.Error == null;
			}
			catch (Exception) { }
			return true;
	}
}
#endif

