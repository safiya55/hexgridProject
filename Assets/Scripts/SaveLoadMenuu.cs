using UnityEngine;
using UnityEngine.UI;
public class SaveLoadMenu : MonoBehaviour {

    public HexGrid hexGrid;
    public Text menuLable, actionButtonLabel;

    public void Open(){
        gameObject.SetActive(true);
        HexMapCamera.Locked = true; 
    }

    public void Close(){
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }
    
    bool saveMode;

    public void Open(bool saveMode){
        this.saveMode = saveMode;
        if(saveMode){
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else{
            menuLable.text = "Load Map";
            actionButtonLabel.text ="Load";
        }
        gameObject.SetActive(true);
        HexMapCamera.Locked = true ;
    }


}


