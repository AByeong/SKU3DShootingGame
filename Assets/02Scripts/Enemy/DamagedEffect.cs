using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DamagedEffect : MonoBehaviour
{
    [SerializeField] private List<Material> _materials = new List<Material>();
    [SerializeField] private List<Color> _originalColors = new List<Color>();
    public float ColorChangeTime = 0.1f;
    [SerializeField] private float _emissionIntensity = 1f; // 방출 강도 조절 파라미터
    private float _timer;
    private bool _isChangingColor = false;

    // 자신과 자식들의 모든 렌더러에서 모든 머티리얼을 찾아 리스트에 저장
    public void FindAllMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        _materials.Clear();
        _originalColors.Clear();

        foreach (Renderer renderer in renderers)
        {
            if (renderer.materials != null && renderer.materials.Length > 0)
            {
                foreach (Material material in renderer.materials)
                {
                    _materials.Add(material);
                    _originalColors.Add(material.color);
                }
            }
        }
    }

    public void StartColorChange()
    {
        if (_isChangingColor) return;
        if (ColorChangeTime <= 0f) return;

        FindAllMaterials(); // 머티리얼 리스트 업데이트

        if (_materials.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name} 또는 자식 오브젝트에 렌더러가 없습니다.", this);
            return;
        }

        _timer = 0f;
        _isChangingColor = true;

        // 모든 머티리얼의 색상을 빨간색으로 변경하고 코루틴 시작
        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i] != null)
            {
                _materials[i].color = Color.red;
                // Emission 활성화 및 강도 1로 설정
                if (_materials[i].HasProperty("_EmissionColor"))
                {
                    _materials[i].EnableKeyword("_EMISSION");
                    _materials[i].SetColor("_EmissionColor", Color.red * _emissionIntensity);
                }
            }
        }

        StartCoroutine(ChangeColorCoroutine());
    }

    private IEnumerator ChangeColorCoroutine()
    {
        while (_timer < ColorChangeTime)
        {
            _timer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(_timer / ColorChangeTime);

            // Emission 강도 감소 (1에서 0으로)
            float currentEmissionIntensity = Mathf.Lerp(_emissionIntensity, 0f, normalizedTime);
            Color currentEmissionColor = Color.red * currentEmissionIntensity;

            // 모든 머티리얼 색상 및 Emission 업데이트
            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != null)
                {
                    // 색상은 처음 빨간색으로 유지
                    if (_materials[i].HasProperty("_EmissionColor"))
                    {
                        _materials[i].SetColor("_EmissionColor", currentEmissionColor);
                        if (currentEmissionIntensity <= 0f)
                        {
                            _materials[i].DisableKeyword("_EMISSION");
                        }
                        else
                        {
                            _materials[i].EnableKeyword("_EMISSION");
                        }
                    }
                }
            }

            yield return null;
        }

        // 색상 변경 완료 후 원래 색상으로 복원 및 Emission 비활성화
        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i] != null && i < _originalColors.Count)
            {
                _materials[i].color = _originalColors[i];
                if (_materials[i].HasProperty("_EmissionColor"))
                {
                    _materials[i].DisableKeyword("_EMISSION");
                }
            }
        }

        _isChangingColor = false;
    }
}