using UnityEngine;
using System.IO;

[System.Serializable]
public struct HexCoordinates
{
    // Serialize the fields so that Unity can display them in the Inspector
    [SerializeField]
    private int x, z;

    // Property to access the X value
    public int X { get { return x; } }

    // Property to access the Z value
    public int Z { get { return z; } }


    // Y coordinate is derived from X and Z, no need to store it directly
    public int Y
    {
        get { return -X - Z; }
    }

    public HexCoordinates(int x, int z)
    {
        if (HexMetrics.Wrapping) {
			int oX = x + z / 2;
			if (oX < 0) {
				x += HexMetrics.wrapSize;
			}
			else if (oX >= HexMetrics.wrapSize) {
				x -= HexMetrics.wrapSize;
			}
		}
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        // Calculate the x-coordinate based on the world position's x value
        float x = position.x / HexMetrics.innerDiameter;

        // The y-coordinate is simply the negative of x to ensure the staggered pattern
        float y = -x;

        // Calculate the offset for the z-coordinate (staggering every two rows)
        float offset = position.z / (HexMetrics.outerRadius * 3f);

        // Adjust x and y for the offset
        x -= offset;
        y -= offset;

        // Round the x and y values to the nearest integers
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);

        // Calculate the z-coordinate based on the rule X + Y + Z = 0
        int iZ = Mathf.RoundToInt(-x - y);

        // Check for rounding errors to ensure X + Y + Z = 0
        if (iX + iY + iZ != 0)
        {
            // Calculate the delta for each coordinate
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            // Discard the coordinate with the largest rounding delta and reconstruct it
            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        // Return the final hex coordinates
        return new HexCoordinates(iX, iZ);
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    // Override ToString to display coordinates on a single line
    public override string ToString()
    {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    // Add a method to display coordinates on separate lines
    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public int DistanceTo(HexCoordinates other)
    {
        return //add up the distances in all three dimensions
            (x < other.x ? other.x - x : x - other.x) +
            (Y < other.Y ? other.Y - Y : Y - other.Y) +
            (z < other.z ? other.z - z : z - other.z) / 2; // divide by 2 to half the sum 
    }

    //to remember which cells they are occupying
    //by storing the coordinates of their locations
    //storing x and z field
    public void Save(BinaryWriter writer)
    {
        writer.Write(x);
        writer.Write(z);
    }

    // static method that reads and returns the stored coordinates.
    public static HexCoordinates Load(BinaryReader reader)
    {
        HexCoordinates c;
        c.x = reader.ReadInt32();
        c.z = reader.ReadInt32();
        return c;
    }
}
