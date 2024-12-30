using UnityEngine;

public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

public static class HexDirectionExtensions
{

    //To get the opposite direction, add 3 to the original direction. 
    //only works for the first three directions though
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }


    //to jump from previous to next direction
    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    public static HexDirection Previous2 (this HexDirection direction) {
		direction -= 2;
		return direction >= HexDirection.NE ? direction : (direction + 6);
	}

	public static HexDirection Next2 (this HexDirection direction) {
		direction += 2;
		return direction <= HexDirection.NW ? direction : (direction - 6);
	}

}