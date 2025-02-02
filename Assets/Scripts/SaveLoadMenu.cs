using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SaveLoadMenu : MonoBehaviour
{
    public Text menuLabel, actionButtonLabel;

    public TMP_InputField nameInput;


    bool saveMode;

    public HexGrid hexGrid;

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
}