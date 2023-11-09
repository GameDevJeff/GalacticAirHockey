using System;
using UnityEngine;

[CreateAssetMenu(fileName ="Game Stats", menuName ="Game Stats")]
public class GameStats : ScriptableObject
{
    public int p1Score = 0;
    public int p2Score = 0;

    public float gameTimer = 0;

    public Action scoreUpdated;

}
