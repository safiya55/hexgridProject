using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;  // Add this for UI components

public class HexGrid : MonoBehaviour
{
    public Color defaultColor = Color.white;
	public Color touchedColor = Color.green;
    public int width = 6;
    public int height = 6;
    public HexCell cellPrefab;       // HexCell prefab
    public Text cellLabelPrefab;     // Text prefab for cell labels

    HexCell[] cells;
    Canvas gridCanvas;               // Reference to the Canvas
    HexMesh hexMesh;

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();  // Locate the Canvas
		hexMesh = GetComponentInChildren<HexMesh>();
     

        cells = new HexCell[height * width];
        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }
    void Start()
    {
        hexMesh.Triangulate(cells); // This will triangulate the cells
    }

    public void ColorCell(Vector3 position, Color color)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        Debug.Log("touched at " + coordinates.ToString());
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        HexCell cell = cells[index];
        cell.color = color;
        hexMesh.Triangulate(cells);
    }


    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);


        // Instantiate the HexCell
        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;

        // Assign the HexCoordinates to the cell
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // Set the color to the default color
        cell.color = defaultColor;

        //connect cells from east to west direction
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            //connecting to SE tile
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);

                //connect to SW tile
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                }
            }
            //doing same logic for odd rows
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                if (x < width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        // Instantiate and position the label
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);

        // Set the label text to show the coordinates of the cell
        label.text = cell.coordinates.ToStringOnSeparateLines();  // Displaying coordinates using HexCoordinates
    }
}
