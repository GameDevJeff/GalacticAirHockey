using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManger : MonoBehaviour
{
    public int scoreAmount = 1;
    public int winingScore = 10;
    public GameStats gameStats;
    public scoreZone scoreZoneP1;
    public scoreZone scoreZoneP2;
    public GameBounds gameBounds;
    public List<GameObject> playingPieces;

    private List<GameObject> m_piecesOrigins = new();

    private delegate void AnyVoidMethod();
    private AnyVoidMethod voidMethod;
    //Game Stats
    private bool freezeTime = false;


    //Paddle Variable
    private int paddle1Index = -1;
    private int paddle2Index = -1;

    //Puck Variables
    private float puckTimerStagnet = 0.0f;
    private int puckIndex = -1;

    //UI
    [SerializeField] private Canvas mainUI;
    [SerializeField] private Image timeUI;
    [SerializeField] private Image winnerUI;
    [SerializeField] private PanelManager panelManager;

    //Networking
    private NetManager netManager;

    //Camera
    [SerializeField] private GameObject cameraP1;
    [SerializeField] private GameObject cameraP2;

    //Player
    public bool isPlayer1 = true;
    public bool firstTimeConnected = true;

    // Start is called before the first frame update
    void Start()
    {
        netManager = GetComponent<NetManager>();

        foreach (GameObject playingPiece in playingPieces)
        {
            GameObject temp = new();
            temp.transform.SetPositionAndRotation(playingPiece.transform.position, playingPiece.transform.rotation);
            temp.transform.localScale = playingPiece.transform.localScale;
            m_piecesOrigins.Add(temp);
        }

        scoreZoneP1.scoredGoal += ScoreIncrease;
        scoreZoneP2.scoredGoal += ScoreIncrease;
        scoreZoneP1.scoredGoal += netManager.NetworkScoreUpdate;
        scoreZoneP2.scoredGoal += netManager.NetworkScoreUpdate;

        panelManager.closedWindow += ClosedWinDiologue;

        gameBounds.outOfBounds += OutOfBoundsObject;

        for (int i = 0; i < playingPieces.Count; i++)
        {
            if (playingPieces[i].GetComponent<PuckLogic>() != null)
                puckIndex = i;
        }
        if (puckIndex == -1)
            Debug.LogError("Puck not found in Playing Pieces List.");

        cameraP1.SetActive(true);
        cameraP2.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (StagnetPuck())
            ResetPuck();

        if (!freezeTime)
            timeUI.GetComponentInChildren<Text>().text = TimeFormat(gameStats.gameTimer += Time.deltaTime);

        //WhichPlayer();

        SetPlayer(isPlayer1);

        UpdateClassesIfOnNet();

    }

    public void ScoreIncrease(bool player1)
    {
        if (player1)
            gameStats.p1Score += scoreAmount;
        else
            gameStats.p2Score += scoreAmount;

        ResetPuck();
        GameWon(CheckForWinningPlayer());

        gameStats?.scoreUpdated.Invoke();
    }

    private void ResetPuck()
    {
        playingPieces[puckIndex].transform.SetPositionAndRotation(m_piecesOrigins[puckIndex].transform.position, m_piecesOrigins[puckIndex].transform.rotation);
        playingPieces[puckIndex].GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    private void ResetPaddle()
    {
        for (int i = 0; i < playingPieces.Count; i++)
        {
            if (playingPieces[i].GetComponent<PuckControls>() != null)
            {
                playingPieces[i].transform.SetPositionAndRotation(m_piecesOrigins[i].transform.position, m_piecesOrigins[i].transform.rotation);
                playingPieces[i].GetComponent<Rigidbody>().velocity = Vector3.zero;
                playingPieces[i].GetComponent<PuckControls>().DisablePaddle();
            }
        }
    }

    IEnumerator DelayedMethod(AnyVoidMethod voidMethod, float delay = 1.0f)
    {
        yield return new WaitForSeconds(delay);
        voidMethod();
        voidMethod -= voidMethod;
    }

    private void ResetScoreAndTime()
    {
        gameStats.p1Score = gameStats.p2Score = 0;
        gameStats.gameTimer = 0f;
        if (netManager.IsConnected)
        {
            netManager.ResetNetScores();
            netManager.ResetNetTimer();
        }
        gameStats.scoreUpdated.Invoke();
    }

    private int CheckForWinningPlayer()
    {
        if (gameStats.p1Score >= winingScore)
            return 2;
        else if (gameStats.p2Score >= winingScore)
            return 1;
        else
            return 0;
    }

    public void GameWon(int player)
    {
        if (player == 0)
            return;

        FreezeGame(true, true);
        winnerUI.GetComponentInChildren<Text>().text = "Player " + player;
        winnerUI.gameObject.SetActive(true);
    }

    private void ClosedWinDiologue(GameObject gameObject)
    {
        if (gameObject = winnerUI.gameObject)
            FreezeGame(false, true);
    }
    public void ResetGame()
    {
        FreezeGame(false, false);
        ResetPuck();
        ResetPaddle();
        ResetScoreAndTime();
    }

    private bool StagnetPuck()
    {
        if (puckTimerStagnet == 0.0f &&
            Mathf.Abs(playingPieces[puckIndex].transform.position.x - m_piecesOrigins[puckIndex].transform.position.x) < 0.2f &&
            Mathf.Abs(playingPieces[puckIndex].transform.position.z - m_piecesOrigins[puckIndex].transform.position.z) < 0.2f)
        {
            return false;
        }

        if (Mathf.Abs((playingPieces[puckIndex].GetComponent<PuckLogic>().positionPrevious - playingPieces[puckIndex].transform.position).magnitude) < 0.003f)
        {

            puckTimerStagnet += Time.deltaTime;
            if (puckTimerStagnet > 4.0f)
            {
                puckTimerStagnet = 0.0f;
                return true;
            }
        }
        else
            puckTimerStagnet = 0.0f;

        return false;
    }

    private void OutOfBoundsObject(GameObject objectToFix)
    {
        if (objectToFix.GetComponent<PuckLogic>())
        {
            voidMethod += ResetPuck;
            StartCoroutine(DelayedMethod(voidMethod, 2f));
        }

        if (objectToFix.GetComponent<PuckControls>())
        {
            voidMethod += ResetPaddle;
            StartCoroutine(DelayedMethod(voidMethod));
        }

    }

    private void OnDestroy()
    {
        scoreZoneP1.scoredGoal -= ScoreIncrease;
        scoreZoneP2.scoredGoal -= ScoreIncrease;
        scoreZoneP1.scoredGoal -= netManager.NetworkScoreUpdate;
        scoreZoneP2.scoredGoal -= netManager.NetworkScoreUpdate;

        panelManager.closedWindow += ClosedWinDiologue;
    }

    private string TimeFormat(float millisecs)
    {
        int seconds = (int)(millisecs);
        int minutes = seconds / 60;
        int milliseconds = ((int)(millisecs * 1000)) % 1000;
        seconds %= 60;
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    public void SetPlayer(bool isPlayerOne)
    {
        if (!isPlayerOne)//p2 postion
        {
            cameraP1.SetActive(false);
            cameraP2.SetActive(true);
            Camera cameraComponentP2 = cameraP2.GetComponent<Camera>();
            mainUI.worldCamera = cameraComponentP2;
            SetPaddleCamera(cameraComponentP2);
            SetCPUForP2(!netManager.existingPlayer.Value, true);
            SetCPUForP2(netManager.existingPlayer.Value, false);
            
        }
        else //p1 position
        {
            cameraP1.SetActive(true);
            cameraP2.SetActive(false);
            Camera cameraComponentP1 = cameraP1.GetComponent<Camera>();
            mainUI.worldCamera = cameraComponentP1;
            SetPaddleCamera(cameraComponentP1);
            SetCPUForP2(!netManager.existingPlayer.Value, false);
            SetCPUForP2(netManager.existingPlayer.Value, true);

        }

    }

    private void SetPaddleCamera(Camera camera)
    {
        for (int i = 0; i < playingPieces.Count; i++)
        {
            PuckControls puckControls = playingPieces[i].GetComponent<PuckControls>();
            if (puckControls != null)
            {
                puckControls.camera = camera;
            }
        }
    }

    private void SetCPUForP2(bool isCPU,bool p1Position)
    {
        bool isPaddle = p1Position;
        for (int i = 0; i < playingPieces.Count; i++)
        {
            PuckControls puckControls = playingPieces[i].GetComponent<PuckControls>();
            if (puckControls != null)
            {
                if (isPaddle)
                {
                    puckControls.CPUPlayer = isCPU;
                    puckControls.CPUOnWhichSide(!p1Position);
                    break;
                }
                isPaddle = true;
            }
        }
    }

    private void FreezeGame(bool freezingPieces, bool freezingTime)
    {
        freezeTime = freezingTime;
        for (int i = 0; i < playingPieces.Count; i++)
        {
            PuckControls puckControls = playingPieces[i].GetComponent<PuckControls>();
            if (puckControls != null)
            {
                puckControls.stopPaddle = freezingPieces;
            }
        }
        playingPieces[puckIndex].GetComponent<PuckLogic>().stopPuck = freezingPieces;
    }

    public Vector3 GetPaddleVelocityForServer(bool isPlayer1)
    {
        int paddle = -1;
        bool currentPaddle = isPlayer1;
        for (int i = 0; i < playingPieces.Count; i++)
        {
            PuckControls puckControls = playingPieces[i].GetComponent<PuckControls>();
            if (puckControls != null)
            {
                if (currentPaddle)
                {
                    paddle = i;
                    break;
                }
                currentPaddle = true;
            }
        }

        //Debug.Log("Game Manger Get Vec3: " + playingPieces[paddle].GetComponent<PuckControls>().velocityForNet);
        return playingPieces[paddle].GetComponent<PuckControls>().velocityToNet;
    }

    public void UpdatePaddleVelocity(int paddleIndex, Vector3 velocity)
    {
        Debug.Log("SRPC Velocity: " + velocity);
        PuckControls puckControls = playingPieces[paddleIndex].GetComponent<PuckControls>();
        if (puckControls != null)
        {
            puckControls.velocityFromNet = velocity;
            puckControls.sentVelocity = true;
        }
    }

    private void UpdateClassesIfOnNet()
    {
        for (int i = 0; i < playingPieces.Count; i++)
        {
            PuckControls puckControls = playingPieces[i].GetComponent<PuckControls>();
            if (puckControls != null)
            {

                puckControls.onNet = netManager.IsConnected;
            }
        }

    }

    private void WhichPlayer()
    {
        if (netManager.IsConnected)
        {
            if (firstTimeConnected)
            {
                isPlayer1 = !netManager.existingPlayer.Value;
                firstTimeConnected = false;
            }
        }
        else
            isPlayer1 = true;

        if (!netManager.IsConnected)
            firstTimeConnected = true;
    }

    public void MakePlayer1(bool bePlayer1)
    {
        isPlayer1 = bePlayer1;
    }
}
