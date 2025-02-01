using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class SaveLoadMenu : MonoBehaviour
{
    public Text menuLabel, actionButtonLabel;

    bool saveMode;

    public InputField nameInput;

    public RectTransform listContent;

    public SaveLoadItem itemPrefab;

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
        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    string GetSelectedPath(){
        string mapName = nameInput.text;
        if(mapName.Length == 0){
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    void Load(string path){
        if (!File.Exists(path)) {
            Debug.Log("File does not exist" + path);
            return;
        }
    }

    public void Action(){
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }
        if(saveMode){
            Save(path);
        }
        else{
            Load(path);
        }
        Close();
    }

    public void SelectItem(string name){
        nameInput.text = name;
    }

    void FillList(){
        for(int i = 0; i < listContent.childCount; i++){
            Destroy(listContent.GetChild(i).gameObject);
        }
        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for(int i = 0; i < paths.Length; i++){
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Paths.GetFileNameWithoutExtention(paths[i]);
            item.transform.SetParent(listContent, false);
        }
    }
}