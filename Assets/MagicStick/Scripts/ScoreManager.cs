using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddScore(int scoreToAdd)
    {
        Score += scoreToAdd;
        UIManager.Instance.UpdateScore(Score);
    }

    public void ResetScore(int scoreToReset)
    {
        Score = scoreToReset;
        UIManager.Instance.UpdateScore(Score);
    }

    public void AddCombo(int comboToAdd)
    {
        Combo += comboToAdd;
        if (Combo > MaxCombo)
        {
            MaxCombo = Combo;
        }
        UIManager.Instance.UpdateCombo(Combo);
    }

    public void ResetCombo(int comboToReset)
    {
        Combo = comboToReset;
        UIManager.Instance.UpdateCombo(Combo);
    }

}
