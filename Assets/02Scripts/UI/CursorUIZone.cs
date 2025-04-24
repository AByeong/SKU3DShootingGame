using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CursorInUIZone : MonoBehaviour
{
    public RectTransform uiZone;
    public bool InUIZone = false;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined; // 마우스 이동은 가능하게
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(uiZone, mousePos);

        InUIZone = false;
    }
}