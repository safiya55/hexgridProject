using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexGridChunk chunk;
    public HexCoordinates coordinates;
    public Color color;
    private int elevation = int.MinValue;
    public RectTransform uiRect;

    [SerializeField]
    HexCell[] neighbors;

    //Whenever a cell is refreshed, it simply refreshes its chunk.
    void Refresh () {
		if (chunk) {
			chunk.Refresh();
            //refresh the chunks of all neighbors
            for (int i = 0; i < neighbors.Length; i++) {
				HexCell neighbor = neighbors[i];
				if (neighbor != null && neighbor.chunk != chunk) {
					neighbor.chunk.Refresh();
				}
			}
		}
	}

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        //set the neighbor in the opposite direction as well
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            if (elevation == value) {
				return;
			}
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            // applies this perturbation to the cell's vertical position
            position.y += 
				(HexMetrics.SampleNoise(position).y * 2f - 1f) *
				HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z =  -position.y;
            uiRect.localPosition = uiPosition;
            Refresh();
            
        }
    }

    public Color Color {
		get {
			return color;
		}
		set {
			if (color == value) {
				return;
			}
			color = value;
			Refresh();
		}
	}

    //to get a cell's edge type in a certain direction.
    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation
        );
    }

    public Vector3 Position {
		get {
			return transform.localPosition;
		}
	}
}
