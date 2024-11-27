using UnityEngine;

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
        this.x = x;
        this.z = z;
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
}
