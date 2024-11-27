using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;  // Add this for UI components

public class HexGrid : MonoBehaviour
{
    public Color defaultColor = Color.white;
	public Color touchedColor = Color.magenta;
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

    void Update()
    {
        // Detect left mouse button click
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        // Create a ray from the camera to the mouse position
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        // Check if the ray hits any collider in the scene
        if (Physics.Raycast(inputRay, out hit))
        {
            TouchCell(hit.point); // Pass the hit point to TouchCell
        }
    }

    public void TouchCell(Vector3 position)
    {
        // Convert world space position to grid space
        position = transform.InverseTransformPoint(position);

        // Find the closest cell using the HexCoordinates class
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);

       // Calculate the array index for the touched cell in the grid
         int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;

        // Get the cell at the calculated index
        HexCell cell = cells[index];

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

        // Instantiate and position the label
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);

        // Set the label text to show the coordinates of the cell
        label.text = cell.coordinates.ToStringOnSeparateLines();  // Displaying coordinates using HexCoordinates
    }
}
