using UnityEngine;
using UnityEngine.UI;

// Tu dong ban su kien GameEventBus.RaiseUIButtonClicked khi button duoc click
[RequireComponent(typeof(Button))]
public class UIButtonAudio : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        GameEventBus.RaiseUIButtonClicked();
    }
}
