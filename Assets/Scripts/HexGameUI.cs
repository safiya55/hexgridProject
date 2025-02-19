using UnityEngine;

public class HexGameUII : MonoBehaviour
{
    public HexGrid grid;

    //game UI should be enabled when we're not in edit mode. Also, this is the place to toggle the labels, 
    //because the game UI will work with paths.
    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        grid.ShowUI(!toggle);
    }
}
