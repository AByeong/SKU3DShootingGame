using System;
using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    //카메라 회전 스크립트
    //목표 : 마우스를 조작하면 카메라를 그 방향으로 회전시키고 싶다.
    
   public float RotationSpeed = 15f;

   //카메라 각도는 0도에서부터 시작한다. 라는 기준을 세운다.
   private float _rotationX = 0;
   private float _rotationY = 0;
   
    private void Update()
    {
        
        //구현순서
        //1. 마우스 입력을 받는다. -> 현재는 움직이는 방향을 알려줌
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        Debug.Log($"Mouse X: {mouseX}, Mouse Y: {mouseY}");
        
        //2. 회전한 양만큼 누적해나간다.
        _rotationX += mouseX * RotationSpeed * Time.deltaTime;
        _rotationY -= mouseY * RotationSpeed * Time.deltaTime;
        
        _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);
        
        
        //3. 회전시킨다.
        transform.eulerAngles = new Vector3(_rotationY, _rotationX, 0f);
        
        
    }
}
