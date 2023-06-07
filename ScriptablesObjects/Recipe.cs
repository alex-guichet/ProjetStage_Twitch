using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Recipe")]
public class Recipe : ScriptableObject
{
    [Tooltip("Tag of the Recipe Name in the localized spread sheet")]
    [SerializeField] private string _recipeNameTag;
    [Tooltip("Tag of the Recipe Description in the localized spread sheet")]
    [SerializeField] private string _recipeDescriptionTag;
    [Tooltip("List of all steps in the recipe")]
    [SerializeField] private List<Step> _stepList;

    [Tooltip( "This is the score calculated at the end of the recipe" )]
    public int scoreForThisRecipe;
    
    public string getNameTag => _recipeNameTag;
    public int getStepCount => _stepList.Count;

    public Step GetStepAtIndex( int step_index )
    {
        return _stepList[ step_index ];
    }

    public void ComputeScore()
    {
        scoreForThisRecipe = Mathf.RoundToInt(_stepList.Sum( step => step.scoreForThisStep ) / (float)_stepList.Count);
    }
}

