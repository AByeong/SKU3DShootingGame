using System;
using UnityEngine;

public class CameraQuerterView : MonoBehaviour
{
    public Transform Target;
public Vector3 Offset;
public float Size = 30;
public Camera Camera;
    private void Start()
    { 
        Camera.orthographic = true;
        
        Offset = new Vector3(60, 150, -60);
    }

    private void Update()
    {
         
        Camera.orthographicSize = Size;
        // 쿼터뷰 기본 각도
        
        transform.position = Target.position + Offset;

        // 타겟을 바라보게 회전
        transform.LookAt(Target);
        
    }
}
