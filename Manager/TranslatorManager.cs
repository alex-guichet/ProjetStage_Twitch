using I2.Loc;
using System.Collections.Generic;
using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class TranslatorManager : Singleton<TranslatorManager>
{
    public List<IngredientInfo> _ingredientPrefabList;
    public Dictionary<string, List<TextTranslation>> ingredientDictionary;
    public const string INGREDIENT = "Ingredients";
    public const string DEFAULT = "Default";
    public MenuFlowManager menuFlowManager;
    
    private Dictionary<string, string> activeUsers = new();
    private List<String> _readyTranslationList = new();
    public int previousStepActiveViewersNumber = 0;
    
    private void CreateReadyTranslationList()
    {
        List<string> all_languages = LocalizationManager.GetAllLanguages();
        
        foreach (string language in all_languages)
        {
            string translation = LocalizationManager.GetTranslation( "Ready", overrideLanguage: language );
            if (translation == null)
            {
                continue;
            }
            _readyTranslationList.Add(translation);
        }
    }
    
    public int GetTotalActiveUsers()
    {
        return activeUsers.Count;
    }
    
    public void ClearActiveUsersDictionary(Step step)
    {
        previousStepActiveViewersNumber = activeUsers.Count;
        activeUsers.Clear();
    }
    
    private void AddActiveUser(string viewer_id, string viewer_name)
    {
        if (activeUsers.TryAdd(viewer_id, viewer_name) && menuFlowManager.currentMenuState == MenuState.RECIPE_INTRO)
        {
            LevelManager.Instance.activeViewerCount.text = GetTotalActiveUsers().ToString();
        }
    }
    
    private void CreateIngredientDictionary()
    {
        List<string> all_languages = LocalizationManager.GetAllLanguages();

        ingredientDictionary = new Dictionary<string, List<TextTranslation>>();

        foreach (IngredientInfo ip in _ingredientPrefabList)
        {
            var ingredient_translation_list = new List<TextTranslation>();
            
            foreach( string language in all_languages )
            {
                if (!language.Contains("_"))
                    continue;

                string translation = LocalizationManager.GetTranslation( $"{INGREDIENT}/{ip.localizationTag}", overrideLanguage: language );
                if (translation is null)
                {
                    continue;
                }
                ingredient_translation_list.Add(new TextTranslation(translation, language));
            }
            ingredientDictionary.TryAdd(ip.localizationTag, ingredient_translation_list);
        }
    }
    
    public void TranslateAndSend(string viewer_id, string viewer_name, string sent_text)
    {
        if (menuFlowManager.currentMenuState == MenuState.RECIPE_INTRO)
        {
            int index_list_ready = _readyTranslationList.FindIndex(x => x.Equals(sent_text, StringComparison.InvariantCultureIgnoreCase));
            if (index_list_ready == -1)
            {
                return;
            }
            AddActiveUser(viewer_id, viewer_name);
            return;
        }
        
        if( sent_text == "streamer" )
        {
            return;
        }
        
        AddActiveUser(viewer_id, viewer_name);
        
        StepMode step_mode = LevelManager.Instance.getMode;

        if (step_mode is StepMode.FIRE_MODE or StepMode.HIDDEN_LETTER or StepMode.HIDDEN_INGREDIENT or StepMode.HIDDEN_GAUGE)
        {
            step_mode = StepMode.CLASSIC;
        }

        foreach ( KeyValuePair<string, List<TextTranslation>> ingredient in ingredientDictionary)
        {
            if( sent_text == PlayerPrefs.GetString( "account_name" ) )
            {
                sent_text = "streamer";
            }
            
            List<int> index_in_dictionary = ( from value in ingredient.Value where value.translatedText.Equals( sent_text, StringComparison.InvariantCultureIgnoreCase ) select ingredient.Value.IndexOf( value ) ).ToList();
            if (index_in_dictionary.Count == 0)
            {
                continue;   // Word not found for this ingredient
            }

            IEnumerable<int> kept_index = index_in_dictionary.Where( index => ingredient.Value[ index ].language.Contains( step_mode.ToString(), StringComparison.InvariantCultureIgnoreCase ) );
            if( !kept_index.Any() )
            {
                continue; // Found occurrences do not correspond to wanted language
            }
            
            int index_list = _ingredientPrefabList.FindIndex(ingredient_tag_prefab => ingredient_tag_prefab.localizationTag == ingredient.Key);
            if( index_list == -1 )
            {
                continue;
            }

            IngredientSpawner.Instance.AddIngredient(viewer_name, _ingredientPrefabList[index_list]);
            break;
        }
    }
    
    public static string GetIngredientTranslation( string ingredient_key, [CanBeNull] string overriding_language = null )
    {
        var translation_term = $"{INGREDIENT}/{ingredient_key}";
        return LocalizationManager.GetTranslation( translation_term, overrideLanguage: overriding_language ?? LocalizationManager.CurrentLanguage );
    }

    public static void SetLanguage( string next_language )
    {
        if (!LocalizationManager.GetAllLanguages().Contains(next_language))
            return;
        
        LocalizationManager.CurrentLanguage = next_language;
    }

    public override void Awake()
    {
        base.Awake();
        CreateReadyTranslationList();
        CreateIngredientDictionary();
    }
    
    public void Start()
    {
        LevelManager.Instance.OnStepStart.AddListener(ClearActiveUsersDictionary);
    }
}
/*
[System.Serializable]
public struct IngredientTagPrefab
{
    public string _ingredientTag;
    public IngredientInfo _ingredientInfo;

    public IngredientTagPrefab(string ingredient_tag, IngredientInfo ingredient_info)
    {
        _ingredientTag = ingredient_tag;
        _ingredientInfo = ingredient_info;
    }
}
*/

public struct TextTranslation
{
    public readonly string   translatedText;
    public readonly string language;

    public TextTranslation(string text, string language)
    {
        translatedText = text;
        this.language  = language;
    }
}
