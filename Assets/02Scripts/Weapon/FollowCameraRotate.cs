using UnityEngine;

public class FollowCameraRotate : MonoBehaviour
{
    public bool FollowCamera;

    private void Update()
    {
        if (FollowCamera)
        {
            this.transform.localRotation = Camera.main.transform.localRotation;
        }
    }
}
