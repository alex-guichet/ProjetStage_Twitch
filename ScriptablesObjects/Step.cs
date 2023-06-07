using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public enum StepMode
{
    CLASSIC,
    INVERSED_WORD,
    DOUBLE_LETTER,
    ADDED_LETTER,
    HIDDEN_INGREDIENT,
    HIDDEN_LETTER,
    HIDDEN_GAUGE,
    FIRE_MODE,
}

[CreateAssetMenu(fileName = "Step", menuName = "ScriptableObjects/Step")]
public class Step : ScriptableObject
{
    [Tooltip("Tag of the step name in the localized spread sheet")]
    [SerializeField] private string _stepNameTag;
    [Tooltip("List of all ingredients and their ratio of the step")]
    [SerializeField] private List<IngredientData> _ingredientList;
    [Tooltip("Time Limit of the step (in seconds)")]
    [SerializeField] private int _timeLimit;

    [Tooltip("Mode of the step")] 
    public StepMode _stepMode;
    [Tooltip("Intensity of the fire, only works in fire Mode")]
    [Range(1, 3)]public int fireIntensity;
    [Tooltip( "Range of delay between two activation of an effect (x: min, y: max)" )]
    public Vector2 delay;
    [Tooltip( "Range of duration of the effect (x: min, y: max)" )]
    public Vector2 duration;

    [Tooltip( "This is the score calculated at the end of the step" )]
    public int scoreForThisStep;
    
    private void OnValidate()
    {
        int sum_ratio = _ingredientList.Sum( i => i._ratio );

        if( sum_ratio != 100 )
        {
            Debug.LogError( $"{_stepNameTag}: ratio sum error: {sum_ratio}/100" );
        }
    }

    public int      getTimeLimit => _timeLimit;
    public string   getNameTag   => _stepNameTag;

    public List<IngredientData> getIngredientList => _ingredientList;
}

[Serializable]
public struct IngredientData
{
    [Tooltip("Prefab of the ingredient")]
    public IngredientInfo _ingredientPrefab;
    [Tooltip("Ratio of the ingredient")]
    public int _ratio;
}
