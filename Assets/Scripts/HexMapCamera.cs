using UnityEngine;

public class HexMapCamera : MonoBehaviour {

	Transform swivel, stick;
    float zoom = 1f;
    public float stickMinZoom, stickMaxZoom;

    public float swivelMinZoom, swivelMaxZoom;

	void Awake () {
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}

    void Update () {
		float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (zoomDelta != 0f) {
			AdjustZoom(zoomDelta);
		}

        float xDelta = Input.GetAxis("Horizontal");
		float zDelta = Input.GetAxis("Vertical");
		if (xDelta != 0f || zDelta != 0f) {
			AdjustPosition(xDelta, zDelta);
		}
	}

    void AdjustPosition (float xDelta, float zDelta) {
        
	}
	
	void AdjustZoom (float delta) 
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}
}