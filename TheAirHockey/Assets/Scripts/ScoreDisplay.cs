using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    public GameStats gameStats;
    public bool isPlayer1;

    int currentScore;
    TMP_Text[]  textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh = GetComponentsInChildren<TMP_Text>();
        if (textMesh == null)
            Debug.LogError("Didn't not find TMP_Text component");
        gameStats.scoreUpdated += UpdateUI;
        gameStats.scoreUpdated.Invoke();
    }

    void UpdateUI()
    {
        if (isPlayer1)
            currentScore = gameStats.p1Score;
        else
            currentScore = gameStats.p2Score;

        foreach (TMP_Text tMesh in textMesh)
        {
            if (currentScore > 9999)
                tMesh.text = "Max";
            else if (currentScore < -999)
                tMesh.text = "Min";
            else
                tMesh.text = currentScore.ToString();
        }
    }

    private void OnDestroy()
    {
        gameStats.scoreUpdated -= UpdateUI;
    }
}
