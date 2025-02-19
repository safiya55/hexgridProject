using UnityEngine;

public class HexGameUII : MonoBehaviour
{

    //HexGameUI needs to know which cell is currently underneath the cursor.
    HexCell currentCell;
    public HexGrid grid;

    //game UI should be enabled when we're not in edit mode. Also, this is the place to toggle the labels, 
    //because the game UI will work with paths.
    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        grid.ShowUI(!toggle);
    }

    //bool to updating the current cell, we might like to know whether it has changed.
    bool UpdateCurrentCell()
    {
        //uses HexGrid.GetCell with the cursor ray, to update the field.
        HexCell cell =
            grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }
}
