using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetManager : NetworkBehaviour
{
    public bool IsConnected { get { return Connected; } }

    private bool Connected = false;
    private GameManger gameManger;
    private NetworkVariable<int> scoreP1 = new NetworkVariable<int>(0);
    private NetworkVariable<int> scoreP2 = new NetworkVariable<int>(0);
    private NetworkVariable<float> timer = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> existingPlayer = new NetworkVariable<bool>(false);
    private float checkPlayersTimer = 0.0f;

    private void Start()
    {
        gameManger = GetComponent<GameManger>();
        scoreP1.OnValueChanged += EchoScoreUpdate;
        scoreP2.OnValueChanged += EchoScoreUpdate;
        timer.OnValueChanged += EchoTimerUpdate;
    }

    private void Update()
    {
        CheckNetworkStatus();
        if (IsConnected)
        {
            if (IsServer)
            {
                timer.Value += Time.deltaTime;
                CheckExistingPlayers();
            }

            if (IsClient)
            {
                Vector3 vec3Catch;
                vec3Catch = gameManger.GetPaddleVelocityForServer(gameManger.isPlayer1);
                Vector3 vec3Pass = new();
                vec3Pass.Set(vec3Catch.x, vec3Catch.y, vec3Catch.z);
                //Debug.Log("vec3Pass: " + vec3Pass);
                //Debug.Log("vec3Catch: " + vec3Catch);
                UpdatePaddleServerRPC(gameManger.isPlayer1, vec3Catch);
            }
        }

    }


    [ServerRpc(RequireOwnership = false)]
    public void UpdatePaddleServerRPC(bool isPlayer1, Vector3 passedVelocity, ServerRpcParams serverRpcParams = default)
    {
        if (isPlayer1)
        {
            gameManger.UpdatePaddleVelocity(1, passedVelocity);
        }
        else
            gameManger.UpdatePaddleVelocity(2, passedVelocity);
    }

    public void NetworkScoreUpdate(bool isPlayer1)
    {
        if (!IsConnected)
            return;

        if (isPlayer1)
        {
            scoreP1.Value += 1;
        }
        else
        {
            scoreP2.Value += 1;
        }
        Debug.Log("P1 Score: " + scoreP1);
        Debug.Log("P2 Score: " + scoreP2);

    }

    public void ResetNetScores()
    {
        scoreP1.Value = scoreP2.Value = 0;
    }

    public void ResetNetTimer()
    {
        timer.Value = 0.0f;
    }

    private void EchoScoreUpdate(int value, int value2)
    {
        gameManger.gameStats.p1Score = scoreP1.Value;
        gameManger.gameStats.p2Score = scoreP2.Value;
        gameManger.gameStats?.scoreUpdated.Invoke();
    }

    private void EchoTimerUpdate(float value, float value2)
    {
        gameManger.gameStats.gameTimer = timer.Value;
    }

    private void CheckNetworkStatus()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost ||
            NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsClient)
        {
            Connected = true;
        }
        else
            Connected = false;
    }

    //usenetwork variable about is player 1
    //then have net client check.

    public void CheckExistingPlayers()
    {
        if (checkPlayersTimer > 0.5f)
        {
            if (NetworkManager.ConnectedClients.Count > 1)
                existingPlayer.Value = true;
            else
                existingPlayer.Value = false;
            
            checkPlayersTimer = 0f;
        }

        checkPlayersTimer += Time.deltaTime;
    }
}
