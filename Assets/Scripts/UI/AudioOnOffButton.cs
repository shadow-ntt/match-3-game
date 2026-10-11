using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Nut dieu khien bat / tat am thanh (Turn On / Turn Off) cho BGM hoac SFX
public class AudioOnOffButton : MonoBehaviour
{
    public enum AudioTargetType
    {
        BGM,
        SFX
    }

    [Header("Loai am thanh dieu khien")]
    [SerializeField] private AudioTargetType audioTarget;

    [Header("Thanh phan UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject offOverlay;

    [Header("Mau sac trang thai")]
    [SerializeField] private Color onColor = Color.white;
    [SerializeField] private Color offColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

    private AudioManager audioManager;
    private bool isOn = true;

    public bool IsOn => isOn;

    private void Awake()
    {
        // Awake lay tham chieu button neu chua gan
        if (button == null) button = GetComponent<Button>();
    }

    private void Start()
    {
        // Khoi tao tham chieu Singleton tap trung tai Start
        audioManager = AudioManager.Instance;

        // Dong bo trang thai ban dau tu AudioManager
        bool initialState = (audioTarget == AudioTargetType.BGM)
            ? audioManager.IsBgmOn
            : audioManager.IsSfxOn;

        SetVisualState(initialState);

        button.onClick.AddListener(OnButtonClicked);

        // Dang ky lang nghe su kien dong bo tu AudioManager
        if (audioTarget == AudioTargetType.BGM)
        {
            AudioManager.OnBgmStateChanged += OnAudioStateChangedHandler;
        }
        else
        {
            AudioManager.OnSfxStateChanged += OnAudioStateChangedHandler;
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);

        if (audioTarget == AudioTargetType.BGM)
        {
            AudioManager.OnBgmStateChanged -= OnAudioStateChangedHandler;
        }
        else
        {
            AudioManager.OnSfxStateChanged -= OnAudioStateChangedHandler;
        }
    }

    // Xu ly click: chuyen doi trang thai tuong minh Turn On / Turn Off
    private void OnButtonClicked()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.15f, 0.18f, 8, 1f);

        if (isOn)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }

    // Bat am thanh tuong minh
    public void TurnOn()
    {
        SetState(true);
    }

    // Tat am thanh tuong minh
    public void TurnOff()
    {
        SetState(false);
    }

    // Ap dung trang thai va ra lenh cho AudioManager
    public void SetState(bool state)
    {
        isOn = state;
        SetVisualState(state);

        if (audioTarget == AudioTargetType.BGM)
        {
            audioManager.SetBgm(state);
        }
        else
        {
            audioManager.SetSfx(state);
        }
    }

    // Cap nhat hien thi giao dien theo trang thai
    private void SetVisualState(bool state)
    {
        isOn = state;

        if (iconImage != null)
        {
            iconImage.color = state ? onColor : offColor;
        }

        if (offOverlay != null)
        {
            offOverlay.SetActive(!state);
        }
    }

    // Callback khi trang thai duoc doi tu nguon khac
    private void OnAudioStateChangedHandler(bool newState)
    {
        SetVisualState(newState);
    }
}
