using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
   HexCell[] cells;

	public HexMesh terrain, rivers;
	Canvas gridCanvas;

	void Awake () {
		gridCanvas = GetComponentInChildren<Canvas>();

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
		Triangulate();
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

	public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        terrain.Apply();
        rivers.Apply();
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

            if (cell.HasRiver) //if river triangulate triangle into a channel
            {
                //detect whether there is a river flowing through its edge
                if (cell.HasRiverThroughEdge(direction))
                {
                    //drop the middle edge vertex to the stream bed's height.
                    e.v3.y = cell.StreamBedY;

                    //Triangulating a part that has only the beginning or end of a river is 
                    //different enough that it warrants its own method. So check for it in 
                    //Triangulate and invoke the appropriate method.
                    if (cell.HasRiverBeginOrEnd)
                    {
                        TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                    }
                    else
                    {
                        TriangulateWithRiver(direction, cell, center, e);
                    }
                }
                else
                { //when the cell has a river, but it doesn't flow through the current direction.
                    TriangulateAdjacentToRiver(direction, cell, center, e);
                }

            }
            else //else keep using a traingle fan
            {
                TriangulateEdgeFan(center, e, cell.color);
            }

            if (direction <= HexDirection.SE)
            {
                TriangulateConnection(direction, cell, e);
            }
        }
    }

	//terminate the channel at the center
    void TriangulateWithRiverBeginOrEnd(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    )
    {
        //create a middle edge between the center and outer edge. 
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        // Adjust the middle vertex to match the streambed height
        m.v3.y = e.v3.y;
        // Triangulate geometry
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color); // Riverbanks
        TriangulateEdgeFan(center, m, cell.Color);         // Terminating fan

        // check whether we have an incoming river, to determine the flow direction. 
        //Then we can insert another river quad between the middle and edge.
        bool reversed = cell.HasIncomingRiver;
		TriangulateRiverQuad(
			m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed
		);

        //The part between the center and middle is a triangle, so we cannot use TriangulateRiverQuad. 
        //The only significant difference is that the center vertex sits in the middle of the river. 
        //So its U coordinate is always ½.
        center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
		rivers.AddTriangle(center, m.v2, m.v4);
		if (reversed) {
			rivers.AddTriangleUV(
				new Vector2(0.5f, 0.4f),
				new Vector2(1f, 0.2f), new Vector2(0f, 0.2f)
			);
		}
		else {
			rivers.AddTriangleUV(
				new Vector2(0.5f, 0.4f),
				new Vector2(0f, 0.6f), new Vector2(1f, 0.6f)
			);
		}
    }

	    //to create a channel straight across the cell part
    void TriangulateWithRiver(
            HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
        )
    {
        //If the cell has a river going through the opposite direction as well as 
        //the direction that we're working with, then it must be a straight river.
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            //stretch centre into a line. line need to have same width as channel
            //find the left vertex by moving ¼ of the way from the center to the 
            //first corner of the previous part.
            centerL = center +
               HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;

            //for the right vertex. In this case, we need the second corner of the next part.
            centerR = center +
               HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        //detect the direction of our curving river
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
                (0.5f * HexMetrics.innerToOuter);
        }
        else
        { //Otherwise, let's revert back to a single point by collapsing the center line.
            centerL = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * 0.5f;
            centerR = center;

        }

        //determine the final center by averaging them.
        center = Vector3.Lerp(centerL, centerR, 0.5f);

        //middle line can be found by creating edge vertices between the center and edge.
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f
        );

        //adjust the middle vertex of the middle edge, 
        //as well as the center, so they become channel bottoms.
        m.v3.y = center.y = e.v3.y;

        //use TriangulateEdgeStrip to fill the space between the middle and edge lines.
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(cell.Color, cell.Color, cell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(cell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(cell.Color);

        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(cell.Color, cell.Color, cell.Color);

        //reverse direction when dealing with incoming river
        bool reversed = cell.IncomingRiver == direction;
        TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
		TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);

    }

	    void TriangulateConnection
        (
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
            e1.v5 + bridge
        );

        //close holes that develop in terrain when triangulating connection
        if (cell.HasRiverThroughEdge(direction))
        {
            e2.v3.y = neighbor.StreamBedY;
            TriangulateRiverQuad(
				e1.v2, e1.v4, e2.v2, e2.v4,
				cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
				cell.HasIncomingRiver && cell.IncomingRiver == direction
			);
        }

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
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            //figure out what the lowest cell is.
            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor
                    );
                }
                else
                {
                    //it means that the next neighbor is the lowest cell.
                    //We have to rotate the triangle counterclockwise to keep it correctly oriented.
                    TriangulateCorner(
                        v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
                    );
                }
            }
            // first check already failed, it becomes a contest between the two neighboring cells. 
            // If the edge neighbor is the lowest, then we have to rotate clockwise, otherwise counterclockwise.
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(
                    e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell
                );

            }
            else
            {
                TriangulateCorner(
                    v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
                );
            }
            //terrain.AddTriangle(v2, v5, v5);
            //terrain.AddTriangleColor(cell.color, neighbor.color, nextNeighbor.color);
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

	 void TriangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    )
    {
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.innerToOuter * 0.5f);
            }
            else if (
                    cell.HasRiverThroughEdge(direction.Previous2())
                )
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
			cell.HasRiverThroughEdge(direction.Previous()) &&
			cell.HasRiverThroughEdge(direction.Next2())
		) {
			center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
		}


        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);
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
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }

	 void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        Vector3 v4 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v5 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

        //merging 2 mound meshes

        terrain.AddTriangle(begin, v4, v5);
        terrain.AddTriangleColor(beginCell.color, c3, c4);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v4;
            Vector3 v2 = v5;
            Color c1 = c3;
            Color c2 = c4;
            v4 = HexMetrics.TerraceLerp(begin, left, i);
            v5 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
            terrain.AddQuad(v1, v2, v4, v5);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }


        terrain.AddQuad(v4, v5, left, right);
        terrain.AddQuadColor(c3, c4, leftCell.color, rightCell.color);
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

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

        //If the top edge is a slope, we again need to connect terraces and a cliff.
        //does the bottom part
        TriangulateBoundaryTriangle(
            begin, beginCell, left, leftCell, boundary, boundaryColor
        );

        //completes the top part by terrain.Add a rotated boundary triangle if there is a slope.
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            //Otherwise a simple triangle suffices.
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
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

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

        //If the top edge is a slope, we again need to connect terraces and a cliff.
        //does the bottom part
        TriangulateBoundaryTriangle(
            right, rightCell, begin, beginCell, boundary, boundaryColor
        );

        //completes the top part by terrain.Add a rotated boundary triangle if there is a slope.
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            //Otherwise a simple triangle suffices.
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

	 void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        //triangulating the terraces.
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

	void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color, color, color);
        terrain.AddTriangle(center, edge.v2, edge.v4);
        terrain.AddTriangleColor(color, color, color);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color, color, color);
        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color, color, color);
    }

	void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2
    )
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v4, e2.v2, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);
    }

    void TriangulateRiverQuad (
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
		float y, float v, bool reversed
	) {
		TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
	}
    
    void TriangulateRiverQuad (
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
		float y1, float y2, float v, bool reversed
	) {
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;
		rivers.AddQuad(v1, v2, v3, v4);
        if (reversed) {
			rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
		}
		else {
			rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
		}
	}
}
