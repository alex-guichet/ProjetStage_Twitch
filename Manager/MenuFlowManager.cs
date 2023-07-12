using System.Collections;
using System.Globalization;
using UnityEngine;
using Cinemachine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

public enum MenuState
{
    MAIN_MENU,
    SETTINGS,
    RECIPE_SELECTION,
    RECIPE_STEP,
    RECIPE_INTRO,
    RECIPE_BREAK,
    RECIPE_END_RESULT,
    GAME_PAUSE,
    CREDITS,
    INSTRUCTIONS,
    NONE
}

public class MenuFlowManager : Singleton<MenuFlowManager>
{
    [HideInInspector] public MenuState currentMenuState;

    [SerializeField] private Animator camerasAnimator;
    [SerializeField] private CinemachineVirtualCamera vcamMainMenu;
    [SerializeField] private CinemachineVirtualCamera vcamSettings;
    [SerializeField] private CinemachineVirtualCamera vcamCredits;
    [SerializeField] private CinemachineVirtualCamera vcamRecipeSelection;
    [SerializeField] private CinemachineVirtualCamera vcamInGame;
    [SerializeField] private CinemachineVirtualCamera vcamEndResult;
    [Space]
    [SerializeField] private CanvasGroup MainMenuUI;
    [SerializeField] private GameObject MainMenuButtons;
    [SerializeField] private GameObject SettingsUI;
    [SerializeField] private GraphicRaycaster RecipeSelectionUI;
    [SerializeField] private GameObject RecipeStepUI;
    [SerializeField] private GameObject RecipeIntroUI;
    [SerializeField] private GameObject RecipeBreakUI;
    [SerializeField] private GameObject RecipeEndResultUI;
    [SerializeField] private GameObject GamePauseUI;
    [SerializeField] private GameObject CreditsUI;
    [SerializeField] private GameObject InstructionsUI;
    [Space]
    [SerializeField] private Animator bookAnimator;
    [SerializeField] private GameObject bookContentRecipe;
    [SerializeField] private GameObject bookContentCredits;
    [SerializeField] private TMP_InputField accountNameInputField;

    private                  Recipe     _recipePrefabReference;
    private static readonly  int        IsOpen = Animator.StringToHash( "IsOpen" );

    [HideInInspector] public UnityEvent OnRecipeClosed = new();

    private void Start()
    {
        currentMenuState = MenuState.NONE;
        SwitchMenuState(MenuState.MAIN_MENU);
        accountNameInputField.text = PlayerPrefs.GetString( "account_name" );
    }

    public void OnButtonQuit()
    {
        Application.Quit();
    }
    public void OnButtonSettings()
    {
        SwitchMenuState(MenuState.SETTINGS);

    }
    public void OnButtonRecipeSelection()
    {
        StartCoroutine( TwitchAPIManager.Instance.Connect() );
        SwitchMenuState(MenuState.RECIPE_SELECTION);
    }
    public void OnButtonCredits()
    {
        SwitchMenuState(MenuState.CREDITS);

    }
    public void OnButtonInstructions()
    {
        SwitchMenuState(MenuState.INSTRUCTIONS);

    }
    public void OnButtonBackToMain()
    {
        SwitchMenuState(MenuState.MAIN_MENU);

    }
    public void OnButtonChooseRecipe( Recipe recipe_prefab_reference )
    {
        _recipePrefabReference = recipe_prefab_reference;
        SwitchMenuState(MenuState.RECIPE_INTRO);
    }
    public void OnButtonStartRecipe()
    {
        SwitchMenuState(MenuState.RECIPE_STEP);
        LevelManager.Instance.ReadRecipe(_recipePrefabReference);
    }
    public void OnButtonContinueRecipe()
    {
        SwitchMenuState(MenuState.RECIPE_STEP);
    }

    public void OnAccountNameChanged()
    {
        PlayerPrefs.SetString( "account_name", accountNameInputField.text.ToLower(CultureInfo.InvariantCulture) );
        PlayerPrefs.Save();
    }

    public void SwitchMenuState (MenuState new_state)
    {
        if (new_state == currentMenuState)
            return;

        switch (currentMenuState)
        {
            case MenuState.MAIN_MENU:
                {
                    MainMenuUI.alpha = 0f;
                    MainMenuButtons.SetActive(false);
                    camerasAnimator.speed = 0f;
                }
                break;
            case MenuState.SETTINGS:
                {
                    SettingsUI.SetActive(false);
                }
                break;
            case MenuState.RECIPE_SELECTION:
                {
                    RecipeSelectionUI.enabled = false;
                }
                break;
            case MenuState.RECIPE_STEP:
                {
                    RecipeStepUI.SetActive(false);
                    bookAnimator.SetBool(IsOpen, false);
                }
                break;
            case MenuState.RECIPE_INTRO:
                {
                    RecipeIntroUI.SetActive(false);
                }
                break;
            case MenuState.RECIPE_BREAK:
                {
                    RecipeBreakUI.SetActive(false);
                }
                break;
            case MenuState.RECIPE_END_RESULT:
                {
                    OnRecipeClosed.Invoke();

                    RecipeEndResultUI.SetActive(false);
                }
                break;
            case MenuState.GAME_PAUSE:
                {
                    GamePauseUI.SetActive(false);
                }
                break;
            case MenuState.CREDITS:
                {
                    CreditsUI.SetActive(false);

                    vcamCredits.enabled = false;
                }
                break;
            case MenuState.INSTRUCTIONS:
                {
                    InstructionsUI.SetActive(false);

                    vcamEndResult.enabled = false;
                }
                break;
            default:
                {
                    RecipeSelectionUI.enabled = false;
                    CreditsUI.SetActive(false);
                    GamePauseUI.SetActive(false);
                    InstructionsUI.SetActive(false);
                    RecipeIntroUI.SetActive(false);
                    RecipeStepUI.SetActive(false);
                    SettingsUI.SetActive(false);
                    MainMenuButtons.SetActive(false);
                }
                break;
        }

        currentMenuState = new_state;

        switch (new_state)
        {
            case MenuState.MAIN_MENU:
                {
                    bookContentCredits.SetActive(true);
                    bookContentRecipe.SetActive(false);

                    vcamSettings.enabled = false;
                    vcamRecipeSelection.enabled = false;
                    vcamInGame.enabled = false;
                    vcamMainMenu.enabled = true;
                }
                break;
            case MenuState.SETTINGS:
                {
                    vcamMainMenu.enabled = false;
                    vcamRecipeSelection.enabled = false;
                    vcamInGame.enabled = false;
                    vcamSettings.enabled = true;
                }
                break;
            case MenuState.RECIPE_SELECTION:
                {
                    bookContentCredits.SetActive(false);
                    bookContentRecipe.SetActive(true);

                    vcamMainMenu.enabled = false;
                    vcamSettings.enabled = false;
                    vcamInGame.enabled = false;
                    vcamRecipeSelection.enabled = true;
                }
                break;
            case MenuState.RECIPE_STEP:
                {
                    RecipeStepUI.SetActive(true);
                    bookAnimator.SetBool(IsOpen, true);
                }
                break;
            case MenuState.RECIPE_INTRO:
                {
                    vcamMainMenu.enabled = false;
                    vcamSettings.enabled = false;
                    vcamRecipeSelection.enabled = false;
                    vcamInGame.enabled = true;
                }
                break;
            case MenuState.RECIPE_BREAK:
                {
                    RecipeBreakUI.SetActive(true);
                }
                break;
            case MenuState.RECIPE_END_RESULT:
                {
                    RecipeEndResultUI.SetActive(true);

                    vcamEndResult.enabled = true;
                }
                break;
            case MenuState.GAME_PAUSE:
                {
                    GamePauseUI.SetActive(true);
                }
                break;
            case MenuState.CREDITS:
                {
                    vcamMainMenu.enabled = false;
                    vcamRecipeSelection.enabled = false;
                    vcamInGame.enabled = false;
                    vcamCredits.enabled = true;
                }
                break;
            case MenuState.INSTRUCTIONS:
                {
                    vcamMainMenu.enabled = false;
                    vcamRecipeSelection.enabled = false;
                    vcamInGame.enabled = false;
                    vcamSettings.enabled = true;
                }
                break;
        }

        StartCoroutine ( SwitchStateAfterDelay(new_state) );
    }


    private IEnumerator SwitchStateAfterDelay( MenuState new_state )
    {
        yield return new WaitForSeconds(0.8f);

        switch (new_state)
        {
            case MenuState.MAIN_MENU:
                {
                    camerasAnimator.speed = 1f;

                    MainMenuButtons.SetActive(true);
                    MainMenuUI.alpha = 1f;
                }
                break;
            case MenuState.SETTINGS:
                {
                    SettingsUI.SetActive(true);
                }
                break;
            case MenuState.RECIPE_SELECTION:
                {
                    RecipeSelectionUI.enabled = true;
                }
                break;
            case MenuState.RECIPE_INTRO:
                {
                    RecipeIntroUI.SetActive(true);
                }
                break;
            case MenuState.CREDITS:
                {
                    CreditsUI.SetActive(true);
                }
                break;
            case MenuState.INSTRUCTIONS:
                {
                    InstructionsUI.SetActive(true);
                }
                break;
        }
    }
}
