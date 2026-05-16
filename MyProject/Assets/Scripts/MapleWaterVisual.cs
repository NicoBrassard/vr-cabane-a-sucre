using UnityEngine;

public class MapleWaterVisual : MonoBehaviour
{
    [SerializeField] private Vector3 _bottomScale;
    [SerializeField] private Vector3 _topScale;
    [SerializeField] private Vector3 _bottomPostion;
    [SerializeField] private Vector3 _topPosition;
    [SerializeField] private MeshRenderer _meshRenderer;

    public void UpdateVisual(float fillAmount, float maxFillAmount)
    {
        if (fillAmount > 0)
        {
            if (!_meshRenderer.enabled)
                _meshRenderer.enabled = true;
            float t = Mathf.Clamp01(fillAmount / maxFillAmount);
            transform.localScale = Vector3.Lerp(_bottomScale, _topScale, t);
            Vector3 pos = transform.localPosition;
            pos = Vector3.Lerp(_bottomPostion, _topPosition, t);
            transform.localPosition = pos;
        }
        else
        {
            if (_meshRenderer.enabled)
                _meshRenderer.enabled = false;
            transform.localScale = _bottomScale;
            Vector3 pos = transform.localPosition;
            pos = _bottomPostion;
            transform.localPosition = pos;
        }
    }
}
