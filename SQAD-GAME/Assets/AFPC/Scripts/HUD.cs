using System.Collections;
using AFPC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example HUD class for health, shield, and endurance values.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("References")]
    public Hero hero;
    public Movement movement;
    public Slider slider_Health;
    public Slider slider_Endurance;
    public TextMeshProUGUI errMsgCantJump;
    public CanvasGroup canvasGroup_DamageFX;

    private bool isShowingErrorMessage = false; // Flag to track if the error message is active

    private void Awake()
    {
        if (hero)
        {
            slider_Health.maxValue = hero.lifecycle.referenceHealth;
            slider_Endurance.maxValue = hero.movement.referenceEndurance;
            errMsgCantJump.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (hero)
        {
            slider_Health.value = hero.lifecycle.GetHealthValue();
            slider_Endurance.value = hero.movement.GetEnduranceValue();

            if (movement.currentScene.name != "Wait" && Input.GetKeyDown(KeyCode.Space) && !isShowingErrorMessage)
            {
                StartCoroutine(ShowErrorMessage());
            }
        }
        canvasGroup_DamageFX.alpha = Mathf.MoveTowards(canvasGroup_DamageFX.alpha, 0, Time.deltaTime * 2);
    }

    private IEnumerator ShowErrorMessage()
    {
        isShowingErrorMessage = true;
        errMsgCantJump.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f); // Wait for 1 second
        errMsgCantJump.gameObject.SetActive(false);
        isShowingErrorMessage = false;
    }

    public void DamageFX()
    {
        canvasGroup_DamageFX.alpha = 1;
    }
}
