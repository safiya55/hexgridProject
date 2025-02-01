using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SaveLoadMenu : MonoBehaviour
{
    public Text menuLabel, actionButtonLabel;

    bool saveMode;

    public InputField nameInput;

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

    string GetSelectedPath(){
        string mapeName = nameInput.text;
        if(mapeName.Length == 0){
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapeName + ".map");
    }
}