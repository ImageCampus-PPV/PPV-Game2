using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    public void SetHealth(int currentHp, int maxHp)
    {
        if (_slider == null)
            return;

        _slider.minValue = 0;
        _slider.maxValue = Mathf.Max(1, maxHp);
        _slider.value = Mathf.Clamp(currentHp, 0, maxHp);
    }
}