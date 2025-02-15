using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    public static UIManager I { get; private set; }
    
    [Header("UI Elements")]
    [SerializeField] private GameObject buildingPanel;
    [SerializeField] private GameObject ghostBuilding;
    [SerializeField] private TMP_Text castleHPText;
    [SerializeField] private Slider castleHPBar;
    [SerializeField] private TMP_Text snowText;
    [Header("Sprites")]
    [SerializeField] private Sprite snowmanSprite;
    [SerializeField] private Sprite barracksSprite;
    [SerializeField] private Sprite cannonSprite;
    [SerializeField] private Sprite collectorSprite;
    [Header("Menus")]
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject winOverlay;
    [SerializeField] private GameObject loseOverlay;
    [SerializeField] private float transitionTime;
    [Header("Input")]
    [SerializeField] private KeyCode pauseKey;
    [SerializeField] private KeyCode rotateKey;
    [SerializeField] private KeyCode cancelBuildingKey;
    [Header("SFX")]
    [SerializeField] private AudioClip clickSFX;
    [Header("References")]
    [SerializeField] private Builder builder;
    [SerializeField] private Castle castle;
    
    [HideInInspector] public Building.BuildingType selectionState = Building.BuildingType.None;

    private float m_buildingAngle;
    private Camera m_camera;
    private Image m_ghostBuildingImage;
    private CanvasGroup m_pauseOverlayCanvasGroup;
    private CanvasGroup m_winOverlayCanvasGroup;
    private CanvasGroup m_loseOverlayCanvasGroup;
    private bool m_isPaused;

    private void Awake() {
        if (I == null) {
            I = this;
        }
        else {
            Destroy(this);
        }
        
        ghostBuilding.SetActive(false);
        m_ghostBuildingImage = ghostBuilding.GetComponent<Image>();
        m_camera = Camera.main;
        castleHPBar.maxValue = castle.maxHealth;
        
        pauseOverlay.SetActive(false);
        m_pauseOverlayCanvasGroup = pauseOverlay.GetComponent<CanvasGroup>();
        m_pauseOverlayCanvasGroup.alpha = 0f;
        
        winOverlay.SetActive(false);
        m_winOverlayCanvasGroup = winOverlay.GetComponent<CanvasGroup>();
        m_winOverlayCanvasGroup.alpha = 0f;
        
        loseOverlay.SetActive(false);
        m_loseOverlayCanvasGroup = loseOverlay.GetComponent<CanvasGroup>();
        m_loseOverlayCanvasGroup.alpha = 0f;
    }

    private void Update() {
        castleHPText.text = castle.CurrentHealth.ToString();
        castleHPBar.value = castle.CurrentHealth;
        snowText.text = castle.snow.ToString();
        
        if (Input.GetKeyDown(pauseKey) && selectionState == Building.BuildingType.None)
            OnPausePressed();
        
        HandleSelection();

        if (Input.GetKeyDown(rotateKey)) {
            m_buildingAngle += 90f;
            m_buildingAngle %= 360f;
        }

        if (selectionState == Building.BuildingType.Cannon) {
            ghostBuilding.transform.localEulerAngles = new Vector3(0f, 0f, m_buildingAngle);
        }
        else {
            ghostBuilding.transform.localEulerAngles = Vector3.zero;
        }

        if (!Input.GetMouseButtonDown(0) || selectionState == Building.BuildingType.None) return;

        builder.Build(
            selectionState,
            GetMouseGridPosition(),
            selectionState == Building.BuildingType.Cannon ? m_buildingAngle : 0f
        );
        
        selectionState = Building.BuildingType.None;
        ToggleGhostBuilding();
    }

    private void HandleSelection() {
        if (selectionState == Building.BuildingType.None) return;
        
        SetGhostBuildingSprite(selectionState);
        
        var mousePos = m_camera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        var ghostPos = m_camera.WorldToScreenPoint(SnapToGrid(mousePos, 2));
        ghostBuilding.transform.position = ghostPos;

        if (!Input.GetKeyDown(cancelBuildingKey)) return;
        
        selectionState = Building.BuildingType.None;
        ToggleGhostBuilding();
    }

    private Vector3 SnapToGrid(Vector3 vector, int gridSize) {
        return new Vector3(
            Mathf.Round(vector.x / gridSize) * gridSize,
            Mathf.Round(vector.y / gridSize) * gridSize,
            0f
        );
    }

    private Vector3 GetMouseGridPosition() {
        var mousePos = m_camera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        return SnapToGrid(mousePos, 2);
    }

    private void ToggleGhostBuilding() {
        ghostBuilding.SetActive(!ghostBuilding.activeSelf);
    }
    
    private void SetGhostBuildingSprite(Building.BuildingType sprite) {
        m_ghostBuildingImage.sprite = sprite switch {
            Building.BuildingType.Snowman => snowmanSprite,
            Building.BuildingType.Barracks => barracksSprite,
            Building.BuildingType.Cannon => cannonSprite,
            Building.BuildingType.Collector => collectorSprite,
            _ => m_ghostBuildingImage.sprite
        };
    }
    
    public void OnPausePressed() {
        StopAllCoroutines();
        StartCoroutine(m_isPaused ? Unpause() : Pause());
        SoundEffectsManager.instance.PlaySFXClip(clickSFX, transform.position);
    }
    
    private IEnumerator Pause() {
        m_isPaused = true;

        Time.timeScale = 0f;
        pauseOverlay.SetActive(true);

        for (var f = 0f; f < transitionTime; f += Time.unscaledDeltaTime) {
            var value = f / transitionTime;

            m_pauseOverlayCanvasGroup.alpha = Mathf.Lerp(m_pauseOverlayCanvasGroup.alpha, 1f, value);
            yield return null;
        }
    }

    private IEnumerator Unpause() {
        m_isPaused = false;
        Time.timeScale = 1f;

        for (var f = 0f; f < transitionTime; f += Time.unscaledDeltaTime) {
            var value = f / transitionTime;

            m_pauseOverlayCanvasGroup.alpha = Mathf.Lerp(m_pauseOverlayCanvasGroup.alpha, 0f, value);
            yield return null;
        }

        pauseOverlay.SetActive(false);
    }
    
    public IEnumerator ShowWinPanel() {
        Time.timeScale = 0f;
        winOverlay.SetActive(true);

        for (var f = 0f; f < transitionTime; f += Time.unscaledDeltaTime) {
            var value = f / transitionTime;

            m_winOverlayCanvasGroup.alpha = Mathf.Lerp(m_winOverlayCanvasGroup.alpha, 1f, value);
            yield return null;
        }
    }
    
    public IEnumerator ShowLosePanel() {
        Time.timeScale = 0f;
        loseOverlay.SetActive(true);

        for (var f = 0f; f < transitionTime; f += Time.unscaledDeltaTime) {
            var value = f / transitionTime;

            m_loseOverlayCanvasGroup.alpha = Mathf.Lerp(m_loseOverlayCanvasGroup.alpha, 1f, value);
            yield return null;
        }
    }

    public void OnSnowmanButtonClicked() {
        selectionState = Building.BuildingType.Snowman;
        ToggleGhostBuilding();
        SoundEffectsManager.instance.PlaySFXClip(clickSFX, transform.position);
    }

    public void OnBarracksButtonClicked() {
        selectionState = Building.BuildingType.Barracks;
        ToggleGhostBuilding();
        SoundEffectsManager.instance.PlaySFXClip(clickSFX, transform.position);
    }
    
    public void OnCannonButtonClicked() {
        selectionState = Building.BuildingType.Cannon;
        ToggleGhostBuilding();
        SoundEffectsManager.instance.PlaySFXClip(clickSFX, transform.position);
    }
    
    public void OnCollectorButtonClicked() {
        selectionState = Building.BuildingType.Collector;
        ToggleGhostBuilding();
        SoundEffectsManager.instance.PlaySFXClip(clickSFX, transform.position);
    }

    public void OnNextLevelButtonClicked() {
        SoundEffectsManager.instance.PlaySFXClip(clickSFX, transform.position);
        SceneManager.I.OnLevelCompleted();
    }
    
    public void OnRestartLevelButtonClicked() {
        SoundEffectsManager.instance.PlaySFXClip(clickSFX, transform.position);
        SceneManager.I.ReloadCurrentScene();
    }
}