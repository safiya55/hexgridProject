using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUII : MonoBehaviour
{

    //HexGameUI needs to know which cell is currently underneath the cursor.
    HexCell currentCell;
    public HexGrid grid;

    //Before we can move a unit, 
    // we have to select one first, and keep track of it. 
    HexUnit selectedUnit;

    //game UI should be enabled when we're not in edit mode. Also, this is the place to toggle the labels, 
    //because the game UI will work with paths.
    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        grid.ShowUI(!toggle);
        //clear the path when the edit mode is changed.
        grid.ClearPath();
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

    //When we attempt a selection,
    void DoSelection()
    {

        grid.ClearPath(); 
        // updating the current cell.
        UpdateCurrentCell();
        //If there is a current cell,
        if (currentCell)
        {
            //the unit occupying that cell becomes the selected unit. 
            selectedUnit = currentCell.Unit;
        }
        //else then we end up with no unit selected.
    }

    void Update()
    {
        //when the cursor is not on top of a GUI element.
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            //performs the selection when mouse button 0 is activated. 
            if (Input.GetMouseButtonDown(0))
            {
                DoSelection();
            }

            else if (selectedUnit)
            {
                DoPathfinding();
            }
        }
    }

    //simply updates the current cell and invokes HexGrid.FindPath 
    // if there's a destination. We'll again use a fixed speed of 24.
    void DoPathfinding()
    {
        if (UpdateCurrentCell())
        {
            if (currentCell) {
				grid.FindPath(selectedUnit.Location, currentCell, 24);
			}
			else {
				grid.ClearPath();
			}
        }
    }
}
