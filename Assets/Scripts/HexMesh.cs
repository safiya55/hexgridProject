using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh hexMesh;
    static List<Vector3> vertices = new List<Vector3>();
	static List<Color> colors = new List<Color>();
	static List<int> triangles = new List<int>();

    MeshCollider meshCollider; // Reference to the MeshCollider

    void Awake()
    {
        // Get the MeshFilter component and create a new mesh for the hex grid
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>(); // Ensure MeshCollider is attached
        hexMesh.name = "Hex Mesh";
    }

    public void Triangulate(HexCell[] cells)
    {
        hexMesh.Clear();
        vertices.Clear();
        colors.Clear();
        triangles.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        hexMesh.vertices = vertices.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.colors = colors.ToArray();
        hexMesh.RecalculateNormals();

        // Assign the generated mesh to the MeshCollider AFTER triangulating is done
        meshCollider.sharedMesh = hexMesh;
    }

    void Triangulate(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
    }

    //using direction to identify the parts
    void Triangulate(HexDirection direction, HexCell cell)
    {
        //null error fix
        if (cell)
        {
            Vector3 center = cell.Position;
            EdgeVertices e = new EdgeVertices(
                center + HexMetrics.GetFirstSolidCorner(direction),
                center + HexMetrics.GetSecondSolidCorner(direction)
            );

            TriangulateEdgeFan(center, e, cell.color);

            if (direction <= HexDirection.SE)
            {
                TriangulateConnection(direction, cell, e);
            }
        }
    }

    void TriangulateConnection(
        HexDirection direction, HexCell cell, EdgeVertices e1
    )
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v4 + bridge
        );

        //decide whether to insert terraces or not.
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor);
        }
        else
        {
            //take care of the flats and cliffs.
            TriangulateEdgeStrip(e1, cell.color, e2, neighbor.color);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            //figure out what the lowest cell is.
            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor
                    );
                }
                else
                {
                    //it means that the next neighbor is the lowest cell.
                    //We have to rotate the triangle counterclockwise to keep it correctly oriented.
                    TriangulateCorner(
                        v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
                    );
                }
            }
            // first check already failed, it becomes a contest between the two neighboring cells. 
            // If the edge neighbor is the lowest, then we have to rotate clockwise, otherwise counterclockwise.
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(
                    e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell
                );

            }
            else
            {
                TriangulateCorner(
                    v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
                );
            }
            //AddTriangle(v2, v4, v5);
            //AddTriangleColor(cell.color, neighbor.color, nextNeighbor.color);
        }
    }

    void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell
    )
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        TriangulateEdgeStrip(begin, beginCell.color, e2, c2);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.color);
    }

    //add color data for each triangle
    void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

    void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        //to determine the types of the left and right edges.
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        //Check whether we are in slope-slope-flat
        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }

            //If the right edge is flat, then we have to begin terracing from the left instead of the bottom.
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }

        //If the left edge is flat, then we have to begin from the right.
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }

        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        }
        else
        {
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }

    void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

        //merging 2 mound meshes

        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.color, c3, c4);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }


        AddQuad(v3, v4, left, right);
        AddQuadColor(c3, c4, leftCell.color, rightCell.color);
    }

    //take care of both slope-cliff cases at once.
    //Merging Slopes and Cliffs
    void TriangulateCornerTerracesCliff(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
    {
        //make connection on side of triangle from 2 bottom to side on the right
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);

        //make sure that the interpolators are always positive.
        if (b < 0)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

        //If the top edge is a slope, we again need to connect terraces and a cliff.
        //does the bottom part
        TriangulateBoundaryTriangle(
            begin, beginCell, left, leftCell, boundary, boundaryColor
        );

        //completes the top part by add a rotated boundary triangle if there is a slope.
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            //Otherwise a simple triangle suffices.
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    void TriangulateCornerCliffTerraces(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
    {
        //make connection on side of triangle from 2 bottom to side on the right
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);

        //make sure that the interpolators are always positive.
        if (b < 0)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

        //If the top edge is a slope, we again need to connect terraces and a cliff.
        //does the bottom part
        TriangulateBoundaryTriangle(
            right, rightCell, begin, beginCell, boundary, boundaryColor
        );

        //completes the top part by add a rotated boundary triangle if there is a slope.
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            //Otherwise a simple triangle suffices.
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        //triangulating the terraces.
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

        AddTriangleUnperturbed(Perturb(begin), v2, boundary);
        AddTriangleColor(beginCell.color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            AddTriangleUnperturbed(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        AddTriangleUnperturbed(v2, Perturb(left), boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }

    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color, color, color);
        AddTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color, color, color);
        AddTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color, color, color);
    }

    void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2
    )
    {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);
        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);
        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);
    }

    void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
}
