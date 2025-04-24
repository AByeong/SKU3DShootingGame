using UnityEngine;

public class playerRotate : MonoBehaviour
{
    public PlayerData PlayerData;
   public float RotationSpeed = 150f; //카메라와 똑같이 움직여야함
   private float _rotationX = 0;


   private void Update()
   {


       float mouseX = Input.GetAxis("Mouse X");


       //2. 회전한 양만큼 누적해나간다.
       _rotationX += mouseX * RotationSpeed * Time.deltaTime;


       transform.eulerAngles = new Vector3(0, _rotationX, 0);
   }
}
