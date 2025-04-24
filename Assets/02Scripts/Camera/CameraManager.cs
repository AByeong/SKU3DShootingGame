using UnityEngine;



public class CameraManager : MonoBehaviour
{

    public enum CameraViewState
    {
        FPS,
        TPS,
        QuerterView
        
    }
    
    public CameraQuerterView QuerterView;
    
    public CameraFollow CameraFollow;
    public CameraRotate CameraRotate;
    
    public CameraTPSMove CameraTPSMove;

    public Camera Camera;

    public CameraViewState CameraView = CameraViewState.FPS;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8))//FPS
        {
            FPS_State();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha9))//TPS
        {
            TPS_State();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha0))//QuarterVie
        {
            Querterview_State();
        }
    }


    private void FPS_State()
    {
        CameraView = CameraViewState.FPS;
        
        Camera.orthographic = false;
        QuerterView.enabled = false;
        CameraFollow.enabled = true;
        CameraRotate.enabled = true;
        CameraTPSMove.enabled = false;
    }

    private void TPS_State()
    {
        CameraView = CameraViewState.TPS;
        
        Camera.orthographic = false;
        QuerterView.enabled = false;
        CameraFollow.enabled = false;
        CameraRotate.enabled = false;
        CameraTPSMove.enabled = true;
    }

    

    private void Querterview_State()
    {
        CameraView = CameraViewState.QuerterView;
        
        QuerterView.enabled = true;
        CameraFollow.enabled = false;
        CameraRotate.enabled = false;
        CameraTPSMove.enabled = false;
    }
    
    
}
