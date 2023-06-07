using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LetterType
{
    ALL,
    VOWEL,
    CONSONANT
}
public enum StringPlacement
{
    START,
    END
}

[CreateAssetMenu(fileName = "AddedLetters", menuName = "ScriptableObjects/StepMode/AddedLetters")]
public class AddedLetters : StepModeObject
{
    [Range(2, 10)] public int _numberRepetition;
    public LetterType _lettersImpacted;
}
