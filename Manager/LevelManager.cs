using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using I2.Loc;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;


public class IngredientCurrentData
{
	public readonly string ingredientNameTag;
	public          int    ratio;
	public          int    currentCount;

	public IngredientCurrentData( string name )
	{
		ingredientNameTag = name;
	}
}
[System.Serializable]
public struct StarLayoutGroup
{
	public Recipe recipe;
	public GridLayoutGroup gridLayout;
}

public class LevelManager : Singleton<LevelManager>
{
	[Tooltip( "Reference to book text that needs to be change to the Step instruction" )]
	public Localize recipeStepInstructionLocalize;
	public TMP_Text recipeStepCountText;
	[Tooltip("Delta between current percentages and goal percentages that is considered acceptable")]
	public  int  deltaPercentageToWin;
	[Tooltip("Time that viewers need to hold good percentages for an early end")]
	public float goodPercentEndingTime;
	[Tooltip("Reference to txt_ActiveViewerCount label in Recipe intro panel")]
	public TMP_Text activeViewerCount;
	[Tooltip("Array of every star Layout Group in the scene")]
	public List<StarLayoutGroup> starLayoutGroupList;

	private bool _stepStarted;

	// Current Recipe
	private Recipe _currentRecipe;

	// Current Step
	private int  _currentRecipeStepCount;
	private int  _currentStepIndex = -1;
	private Step _currentStep;

	// Step Timer
	private int   _currentStepTimeLimit;
	private float _currentTimeInStep;

	// Percentage update Timer
	[Tooltip( "Timer between percentage update to avoid spam" )]
	public float timeBetweenUpdates = 1f;
	private float _currentTimeBetweenUpdates;

	// Ingredient goal and current
	private List<IngredientData>        _ingredientsGoalPercentage;
	private List<IngredientCurrentData> _ingredientCurrentPercentage;
	private bool                        _percentageWhereBad;

	[HideInInspector] public UnityEvent<Step>                        OnStepStart        = new();
	[HideInInspector] public UnityEvent<List<IngredientCurrentData>> OnPercentageUpdate = new();
	[HideInInspector] public UnityEvent<Step>                        OnStepEnd          = new();
	[HideInInspector] public UnityEvent<Recipe>                      OnRecipeEnd        = new();
	[HideInInspector] public UnityEvent<float, float>                OnUpdateTime       = new();
	
	private List<string> _recipeLocalizationList;
	public  StepMode     getMode => _currentStep._stepMode;

	// Function called when selecting the recipe in the menu
	public void ReadRecipe( Recipe recipe_to_select )
	{
		_currentRecipe = recipe_to_select;

		if( !_currentRecipe )
		{
			Debug.LogWarning($"Recipe {recipe_to_select} does not exist");
			return;
		}
			
		_recipeLocalizationList = LocalizationManager.GetTermsList( $"Recipes/{_currentRecipe.getNameTag}" );

		_currentRecipeStepCount = _currentRecipe.getStepCount;

		// Launch first step
		ReadStep();
	}

	private void ReadStep()
	{
		if( ++_currentStepIndex >= _currentRecipeStepCount )
		{
			return;
		}

		_currentStep          = _currentRecipe.GetStepAtIndex( _currentStepIndex );

		if( !_currentStep )
		{
			return;
		}
		
		OnStepStart.Invoke(_currentStep);

		_currentStepTimeLimit = _currentStep.getTimeLimit;
		_currentTimeInStep    = 0f;

		recipeStepInstructionLocalize.Term = _recipeLocalizationList[_currentStepIndex + 1];
		recipeStepCountText.text = (_currentStepIndex + 1) + "/" + _currentRecipeStepCount;

		StartStepModEffect();
		
		// Get Ingredients
		_ingredientsGoalPercentage = _currentStep.getIngredientList;

		_ingredientCurrentPercentage?.Clear();
		_ingredientCurrentPercentage = new List<IngredientCurrentData>();

		foreach( IngredientData ingredient_data in _ingredientsGoalPercentage )
			_ingredientCurrentPercentage.Add( new IngredientCurrentData( ingredient_data._ingredientPrefab.localizationTag ) );
		
		_stepStarted = true;
	}

	private void StartStepModEffect()
	{
		switch( _currentStep._stepMode )
		{
			case StepMode.FIRE_MODE:
			{
				FireModeManager.Instance.ActivateFireMode(_currentStep.fireIntensity);
				break;
			}
			
			case StepMode.HIDDEN_GAUGE:
			{
				TubesController.Instance.StartHidingTubes(_currentStep.delay, _currentStep.duration);
				FireModeManager.Instance.ActivateFireMode(_currentStep.fireIntensity);
				break;
			}

			case StepMode.HIDDEN_INGREDIENT:
			{
				TubesController.Instance.StartHidingNames(_currentStep.delay, _currentStep.duration);
				FireModeManager.Instance.ActivateFireMode(_currentStep.fireIntensity);
				break;
			}

			case StepMode.CLASSIC:
			case StepMode.INVERSED_WORD:
			case StepMode.DOUBLE_LETTER:
			case StepMode.ADDED_LETTER:
			case StepMode.HIDDEN_LETTER:
			default:
				FireModeManager.Instance.ActivateFireMode(_currentStep.fireIntensity);
				break;
		}
	}

	private void EndRecipe()
	{
		_currentStepIndex = -1;

		_currentRecipe.ComputeScore();
		SaveScoreAndUpdate(_currentRecipe.getNameTag, _currentRecipe.scoreForThisRecipe);
		MenuFlowManager.Instance.SwitchMenuState(MenuState.RECIPE_END_RESULT);

		OnRecipeEnd.Invoke(_currentRecipe);
	}

	public void InitializeAllStarLayouts()
	{
		foreach (StarLayoutGroup s in starLayoutGroupList)
		{
			string recipeTag = s.recipe.getNameTag;
			string key = recipeTag + "_score";
			if (!PlayerPrefs.HasKey(key))
			{
				continue;
			}
			UpdateStarLayoutGroup(recipeTag,PlayerPrefs.GetInt(key)); 
		}
	}

	public void UpdateStarLayoutGroup(string recipeTag, int starNumber)
	{
		int index = starLayoutGroupList.FindIndex(x => x.recipe.getNameTag.Equals(recipeTag));

		if (index == -1)
			return;

		int currentStarsActive = 0;
		Transform GridLayoutTransform = starLayoutGroupList[index].gridLayout.gameObject.transform;
	
		for (int i = 0; i < GridLayoutTransform.childCount; i++)
		{
			if (GridLayoutTransform.GetChild(i).gameObject.activeInHierarchy)
			{
				GridLayoutTransform.GetChild(i).gameObject.SetActive(false);
			}
		}
		
		for (int i = 0; i < starNumber; i++)
		{
			GridLayoutTransform.GetChild(i).gameObject.SetActive(true);
		}

	}

	public void SaveScoreAndUpdate(string recipe_tag, int current_score)
	{
		string current_key = recipe_tag + "_score";
		
		if (!PlayerPrefs.HasKey(current_key))
		{
			PlayerPrefs.SetInt(current_key, current_score);
			UpdateStarLayoutGroup(recipe_tag, current_score);
			return;
		}
		
		if (current_score >= PlayerPrefs.GetInt(current_key, current_score))
		{
			PlayerPrefs.SetInt(current_key, current_score);
			UpdateStarLayoutGroup(recipe_tag, current_score);
		}
	}

	public static void StartStep()
	{
		Instance.StartStepIntern();
	}

	private void StartStepIntern()
	{
		if( _currentStepIndex == _currentRecipeStepCount - 1 )
		{
			EndRecipe();
			return;
		}

		ReadStep();
	}

	private void EndStep()
	{
		_stepStarted       = false;
		_currentTimeInStep = 0f;
		
		MenuFlowManager.Instance.SwitchMenuState(MenuState.RECIPE_BREAK);
		
		OnStepEnd.Invoke(_currentStep);
	}

	public void AddIngredient( string ingredient_to_increase )
	{
		if (!_stepStarted)
			return;

		IngredientCurrentData ingredient_current_data = _ingredientCurrentPercentage.Find( x => x.ingredientNameTag == ingredient_to_increase );

		if( ingredient_current_data is null )
			return;
		
		ingredient_current_data.currentCount += 1;
	}

	private void UpdatePercentages()
	{
		int total_count = _ingredientCurrentPercentage.Sum( ingredient => ingredient.currentCount );

		foreach( IngredientCurrentData ingredient_data in _ingredientCurrentPercentage )
			ingredient_data.ratio = Mathf.RoundToInt( 100f * ingredient_data.currentCount / total_count );

		_currentTimeBetweenUpdates = 0f;
		
		OnPercentageUpdate.Invoke(_ingredientCurrentPercentage);
	}

	private bool CheckGoodPercentage()
	{
		float[] deltas = GetPercentageDelta();

		int exceeding_delta_count = deltas.Count( delta => delta > deltaPercentageToWin );

		return exceeding_delta_count == 0;
	}

	private float[] GetPercentageDelta()
	{
		var deltas = new float[_ingredientCurrentPercentage.Count];
		for( var i = 0; i < _ingredientCurrentPercentage.Count; i++)
		{
			 deltas[i] = Mathf.Abs( _ingredientCurrentPercentage[i].ratio - _ingredientsGoalPercentage.Find( x => x._ingredientPrefab.localizationTag ==  _ingredientCurrentPercentage[i].ingredientNameTag )._ratio );
		}

		return deltas;
	}

	public void EarlyEndStep()
	{
		_currentStep.scoreForThisStep = 5;

		EndStep();
	}

	private void TimerEndStep()
	{
		float[] deltas = GetPercentageDelta();

		float average_delta = deltas.Sum() / deltas.Length;

		_currentStep.scoreForThisStep = average_delta switch {
			> 17 => 0,
			> 13 => 1,
			> 9 => 2,
			> 5  => 3,
			> 1  => 4,
			_    => 5,
		};

		EndStep();
	}
	
	private void Update()
	{
		if( !_stepStarted )
			return;

		float delta_time = Time.deltaTime;

		// Update timers
		_currentTimeInStep         += delta_time;
		_currentTimeBetweenUpdates += delta_time;

		OnUpdateTime.Invoke( _currentStepTimeLimit - _currentTimeInStep, _currentStepTimeLimit );
		
		if( _currentTimeBetweenUpdates >= timeBetweenUpdates )
			UpdatePercentages();
		
		// Checking if all percentage are good at this frame but not at the previous
		// If so invoke the early end step five seconds later
		if( CheckGoodPercentage())
		{
			if ( _percentageWhereBad )
			{
				_percentageWhereBad = false;
				StartCoroutine( HoldPercentages( goodPercentEndingTime ) );
			}
		}
		else
			_percentageWhereBad = true;

		// If time spend on the current step reach the time limit => end step
		if( _currentTimeInStep >= _currentStepTimeLimit )
			TimerEndStep();
	}

	public  void Start()
	{
		InitializeAllStarLayouts();
	}
	
	private IEnumerator HoldPercentages( float time )
	{
		var current_time = 0f;

		while( current_time < time )
		{
			yield return null;

			current_time += Time.deltaTime;
			
			// Check if step didn't end in the meantime
			if( !_stepStarted )
				yield break;
			
			if( !CheckGoodPercentage() )
				yield break;
		}

		EarlyEndStep();
	}
}
