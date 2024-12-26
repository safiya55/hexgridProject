using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    Transform swivel, stick;
    public float stickMinZoom, stickMaxZoom;
    public float swivelMinZoom, swivelMaxZoom;

    public float moveSpeed;

    //value of 0 means that we are fully zoomed out,
    // while a value of 1 is fully zoomed in.
    float zoom = 1f;

	void Awake () {
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}

    void Update () {
        //sooming in and out with scroll wheel
		float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (zoomDelta != 0f) {
			AdjustZoom(zoomDelta);
		}

        //camera movement
        float xDelta = Input.GetAxis("Horizontal");
		float zDelta = Input.GetAxis("Vertical");
		if (xDelta != 0f || zDelta != 0f) {
			AdjustPosition(xDelta, zDelta);
		}
	}

    void AdjustPosition (float xDelta, float zDelta) 
    {
        // fetch the current position of the camera rig
        Vector3 position = transform.localPosition;
        //add the X and Z deltas to it
		position += new Vector3(xDelta, 0f, zDelta);
        //assign the result back to the rig's position.
		transform.localPosition = position;
	}
	
	void AdjustZoom (float delta) {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

}
