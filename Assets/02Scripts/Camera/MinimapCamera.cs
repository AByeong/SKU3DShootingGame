using System;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Transform Target;
    private Camera _camera;
    
    [Header("카메라 파라미터")]
    public float Yoffset = 10f;

    public float ZoomInRate = 1f;
    public float ZoomOutRate = 1f;
    
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }
    
    private void LateUpdate()
    {
        transform.position = Target.position + new Vector3(0, Yoffset, 0);
   
        transform.eulerAngles = new Vector3(90, Target.eulerAngles.y, 0);

    }

    public void ZoomIn()
    {
        _camera.orthographicSize += ZoomInRate;
    }


    public void ZoomOut()
    {
        _camera.orthographicSize -= ZoomOutRate;

    }
    
    
}
