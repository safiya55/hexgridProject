using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;



public class SaveLoadMenu : MonoBehaviour
{
	//const int mapFileVersion = 5;

	[SerializeField]
	public Text menuLabel, actionButtonLabel;

	public TMP_InputField nameInput;

	

	//tp fill list
	public RectTransform listContent;

	[SerializeField]
	public SaveLoadItem itemPrefab;

	[SerializeField]
	public HexGrid hexGrid;

	bool saveMode;

	const int mapFileVersion = 5;

	public void Open(bool saveMode)
	{
		this.saveMode = saveMode;

		if (saveMode)
		{
			menuLabel.text = "Save Map";
			actionButtonLabel.text = "Save";
		}
		else
		{
			menuLabel.text = "Load Map";
			actionButtonLabel.text = "Load";
		}

		//fill the list of map
		FillList();
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

	public void Action()
	{
		string path = GetSelectedPath();
		if (path == null)
		{
			return;
		}
		if (saveMode)
		{
			Save(path);
		}
		else
		{
			Load(path);
		}
		Close();
	}

	public void SelectItem(string name) => nameInput.text = name;

	//delete selected maps
	public void Delete () {
		string path = GetSelectedPath();
		if (path == null) {
			return;
		}

		if (File.Exists(path)) {
			File.Delete(path);
		}

		// clear Name Input after deletion
		nameInput.text = "";
		//update menu list
		FillList();
	}

	void FillList()
	{

		//make sure to remove all old items before adding new ones.
		for (int i = 0; i < listContent.childCount; i++)
		{
			Destroy(listContent.GetChild(i).gameObject);
		}

		string[] paths =
			Directory.GetFiles(Application.persistentDataPath, "*.map");

		//display array in the list in alphabetical order
		Array.Sort(paths);


		//create prefab instance of each item in the array
		for (int i = 0; i < paths.Length; i++)
		{
			SaveLoadItem item = Instantiate(itemPrefab);

			//link item to the menu
			item.Menu = this;
			//set map name
			item.MapName = Path.GetFileNameWithoutExtension(paths[i]);

			//make it a child of the list content
			item.transform.SetParent(listContent, false);
		}
	}

	string GetSelectedPath()
	{
		string mapName = nameInput.text;
		if (mapName.Length == 0)
		{
			return null;
		}
		return Path.Combine(Application.persistentDataPath, mapName + ".map");
	}

	public void Save(string path)
	{
		//file path of saved maps
		//Debug.Log(Application.persistentDataPath);
		//write to file
		using (
			BinaryWriter writer =
				new BinaryWriter(File.Open(path, FileMode.Create))
		)
		{
			writer.Write(mapFileVersion);
			hexGrid.Save(writer);
		}
	}

	public void Load(string path)
	{
		//ensure that the file actually exists,
		if (!File.Exists(path))
		{
			Debug.LogError("File does not exist " + path);
			return;
		}

		using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
		{
			int header = reader.ReadInt32();
			// can now correctly load version 2 files,
			if (header <= mapFileVersion)
			{
				hexGrid.Load(reader, header);
				HexMapCamera.ValidatePosition();
			}
			else
			{
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}
}