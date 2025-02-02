using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class SaveLoadMenu : MonoBehaviour
{
    public Text menuLabel, actionButtonLabel;

    public TMP_InputField nameInput;


    bool saveMode;

    public HexGrid hexGrid;
    
    //tp fill list
    public RectTransform listContent;
	
	public SaveLoadItem itemPrefab;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;

        if (saveMode) {
			menuLabel.text = "Save Map";
			actionButtonLabel.text = "Save";
		}
		else {
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

    string GetSelectedPath () {
		string mapName = nameInput.text;
		if (mapName.Length == 0) {
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
			writer.Write(1);
			hexGrid.Save(writer);
		}
	}

	public void Load(string path)
	{
        //ensure that the file actually exists,
        if (!File.Exists(path)) {
			Debug.LogError("File does not exist " + path);
			return;
		}

		using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
		{
			int header = reader.ReadInt32();
			if (header <= 1) {
				hexGrid.Load(reader, header);
				HexMapCamera.ValidatePosition();
			}
			else {
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}

    public void Action () {
		string path = GetSelectedPath();
		if (path == null) {
			return;
		}
		if (saveMode) {
			Save(path);
		}
		else {
			Load(path);
		}
		Close();
	}

    public void SelectItem (string name) {
		nameInput.text = name;
	}

    void FillList () {
        
        //make sure to remove all old items before adding new ones.
        for (int i = 0; i < listContent.childCount; i++) {
			Destroy(listContent.GetChild(i).gameObject);
		}

		string[] paths =
			Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);

        //create prefab instance of each item in the array
        for (int i = 0; i < paths.Length; i++) {
			SaveLoadItem item = Instantiate(itemPrefab);
            
            //link item to the menu
			item.menu = this;
			//set map name
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            //make it a child of the list content
			item.transform.SetParent(listContent, false);
		}
	}
}