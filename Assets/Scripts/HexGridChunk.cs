using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
   HexCell[] cells;

	HexMesh hexMesh;
	Canvas gridCanvas;

	void Awake () {
		gridCanvas = GetComponentInChildren<Canvas>();
		hexMesh = GetComponentInChildren<HexMesh>();

		cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];

		//changed to initially set to true cause otherwise grid just wont show
		ShowUI(true);
	}

    public void Refresh () {
		enabled = true;
	}

    //diff between update and late update
    //Each frame, the Update methods of enabled components are invoked at some point, 
    //in arbitrary order. After that's finished, the same happens with LateUpdate methods. 
    //So there are two update steps, an early and a late one.
    void LateUpdate () {
		hexMesh.Triangulate(cells);
		enabled = false;
	}

    public void AddCell (int index, HexCell cell) {
		cells[index] = cell;
        cell.chunk = this;
		cell.transform.SetParent(transform, false);
		cell.uiRect.SetParent(gridCanvas.transform, false);
	}

	public void ShowUI (bool visible) {
		gridCanvas.gameObject.SetActive(visible);
	}

}
