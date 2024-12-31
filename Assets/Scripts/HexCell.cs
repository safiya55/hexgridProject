using UnityEngine;

public class HexCell : MonoBehaviour
{

    public HexCoordinates coordinates;
    private int elevation = int.MinValue;
    public RectTransform uiRect;

    public HexGridChunk chunk;
    public Color color;
   
	//river info
    bool hasIncomingRiver;
	bool hasOutgoingRiver;
    HexDirection incomingRiver;
	HexDirection outgoingRiver;
	
    [SerializeField]
    HexCell[] neighbors;
    

    void Refresh () {
		if (chunk) {
			chunk.Refresh();
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
            position.y +=
				(HexMetrics.SampleNoise(position).y * 2f - 1f) *
				HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;


			//Preventing Uphill Rivers and remove them
			if (
				hasOutgoingRiver &&
				elevation < GetNeighbor(outgoingRiver).elevation
			) {
				RemoveOutgoingRiver();
			}
			if (
				hasIncomingRiver &&
				elevation > GetNeighbor(incomingRiver).elevation
			) {
				RemoveIncomingRiver();
			}


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

    public bool HasIncomingRiver {
		get {
			return hasIncomingRiver;
		}
	}

	public bool HasOutgoingRiver {
		get {
			return hasOutgoingRiver;
		}
	}

	public HexDirection IncomingRiver {
		get {
			return incomingRiver;
		}
	}

	public HexDirection OutgoingRiver {
		get {
			return outgoingRiver;
		}
	}

    public bool HasRiver {
		get {
			return hasIncomingRiver || hasOutgoingRiver;
		}
	}

    public bool HasRiverBeginOrEnd {
		get {
			return hasIncomingRiver != hasOutgoingRiver;
		}
	}

    public bool HasRiverThroughEdge (HexDirection direction) {
		return
			hasIncomingRiver && incomingRiver == direction ||
			hasOutgoingRiver && outgoingRiver == direction;
	}

    public void RemoveOutgoingRiver () {
		if (!hasOutgoingRiver) {
			return;
		}
		hasOutgoingRiver = false;
		RefreshSelfOnly();

        //removes neighboring rivers
		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

    void RefreshSelfOnly () {
		chunk.Refresh();
	}

    //Removing the incoming river works the same way.
    public void RemoveIncomingRiver () {
		if (!hasIncomingRiver) {
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}

    //removing the entire river just means removing both 
    //the outgoing and incoming river parts.
    public void RemoveRiver () {
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

    public void SetOutgoingRiver (HexDirection direction) {
        //dont do anything if river already exist
		if (hasOutgoingRiver && outgoingRiver == direction) {
			return;
		}

        //ensure that there is a neighbor in the desired direction. 
        //Also, rivers cannot flow uphill. 
        //So we'll have to abort if the neighbor has a higher elevation.
        HexCell neighbor = GetNeighbor(direction);
		if (!neighbor || elevation < neighbor.elevation) {
			return;
		}

        //clear previous outgoing river n remove incoming river, 
        //if it overlaps with new outgoing river.
        RemoveOutgoingRiver();
		if (hasIncomingRiver && incomingRiver == direction) {
			RemoveIncomingRiver();
		}

        //setting the outgoing river.
        hasOutgoingRiver = true;
		outgoingRiver = direction;
		RefreshSelfOnly();

        //set the incoming river of the other cell, 
        //after removing its current incoming river, if any.
        neighbor.RemoveIncomingRiver();
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();
		neighbor.RefreshSelfOnly();
	}

	//to retrieve the vertical position of its stream bed.
	public float StreamBedY {
		get {
			return
				(elevation + HexMetrics.streamBedElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	public float RiverSurfaceY {
		get {
			return// to retrieve the vertical position of its river's surface.
				(elevation + HexMetrics.riverSurfaceElevationOffset) *
				HexMetrics.elevationStep;
		}
	}
	
}
