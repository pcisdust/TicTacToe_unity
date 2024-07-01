using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public enum UiInterfaceEnum 
    { MainMenu, NewGame, CheckerBoard }
    public enum UiSoundEnum
    { Click, Gameover, Reset, Piece }

    public List<CanvasGroup> allUi;
    [Header("new game")]
    public GameObject aiDifficultyPanel;
    public List<Toggle> difficultyToggles;
    public List<Toggle> firstMoveToggles;
    [Header("checker board")]
    public Texture circleTexture;
    public Texture crossTexture;
    public RawImage circleTurn;
    public RawImage crossTurn;
    public List<RawImage> grids;
    [Header("sound")]
    public RawImage soundUi;
    public Texture soundOnTexture;
    public Texture soundOffTexture;
    public AudioClip clickSound;
    public AudioClip gameoverSound;
    public AudioClip resetSound;
    public AudioClip pieceSound;
    public AudioSource audioSource;
    [Header("result")]
    public CanvasGroup result;
    public RawImage resultImage;
    public List<Text> winText;
    public string circleWinText;
    public string crossWinText;
    public string drawText;
    [Header("ai")]
    public List<int> presetStrategy;
    #region private
    private int aiDifficulty = 0;
    private bool isFirstCircleMove = true;
    private bool isCircleMove = false;
    private bool uesAi = false;
    private UiInterfaceEnum curUi;

    private bool nextTurnUpdate = false;
    private float nextTurnTimer = 0f;
    private bool resultUpdate = false;
    private float resultTimer = 0f;
    private bool isSoundOn = true;
    private Color circleColor;
    private Color crossColor;
    private Color tempCircleColor;
    private Color tempCrossColor;
    private bool isGameOver = false;

    private int[,] boardData = new int[3,3];
    private const int emptyData = 0;
    private const int circleData = 1;
    private const int crossData = 2;

    private List<int> allEmpty;
    private int remain = 0;
    private int aiBestMove = 0;
    #endregion
    public void Start()
    {
        MainMenu();
        circleColor = circleTurn.color;
        crossColor = crossTurn.color;
        soundUi.texture = isSoundOn ? soundOnTexture : soundOffTexture;
    }
    public void Update()
    {
        if (nextTurnUpdate)
        {
            nextTurnTimer += Time.deltaTime*2f;
            tempCircleColor.a = isCircleMove ? nextTurnTimer : 1f - nextTurnTimer;
            tempCrossColor.a = isCircleMove ? 1f - nextTurnTimer: nextTurnTimer;
            circleTurn.color = tempCircleColor;
            crossTurn.color = tempCrossColor;
            if (nextTurnTimer>1f) 
            {
                nextTurnUpdate = false;
                circleTurn.color = isCircleMove ? circleColor : Color.clear;
                crossTurn.color = isCircleMove ? Color.clear : crossColor;
                if (!isCircleMove && uesAi)
                {
                    AiAct();
                }
            }
        }
        if (resultUpdate) 
        {
            resultTimer += Time.deltaTime * 2f;
            result.alpha = resultTimer;
            if (resultTimer > 1f) 
            {
                result.alpha = 1f;
                result.blocksRaycasts = true;
                result.interactable = true;
            }
        }
    }

    #region ui
    public void NewGame()
    {
        UiVisibility(UiInterfaceEnum.NewGame);
        uesAi = false;
        aiDifficultyPanel.SetActive(false);
    }
    public void NewAiGame()
    {
        UiVisibility(UiInterfaceEnum.NewGame);
        uesAi = true;
        aiDifficultyPanel.SetActive(true);
    }
    public void GameStart()
    {
        for(int i = 0; i < difficultyToggles.Count; i++) 
        {
            if (difficultyToggles[i].isOn) 
            {
                aiDifficulty = i;break;
            }
        }
        for (int i = 0; i < firstMoveToggles.Count; i++)
        {
            if (firstMoveToggles[i].isOn)
            {
                isFirstCircleMove = (i == 0);
                break;
            }
        }
        NextTurn(isFirstCircleMove);
        UiVisibility(UiInterfaceEnum.CheckerBoard);
    }
    public void Quit()
    {
        if (curUi == UiInterfaceEnum.MainMenu)
        {
#if UNITY_EDITOR

            UnityEditor.EditorApplication.isPlaying = false;
#else

            Application.Quit();
#endif
        }
        else
        {
            MainMenu();
        }
    }
    public void MainMenu()
    {
        GameReset();
        uesAi = false;
        aiDifficulty = 0;
        isFirstCircleMove = true;
        difficultyToggles[0].isOn = true;
        firstMoveToggles[0].isOn = true;
        UiVisibility(UiInterfaceEnum.MainMenu);
    }
    public void SoundSwitch() 
    {
        isSoundOn = !isSoundOn;
        soundUi.texture = isSoundOn ? soundOnTexture : soundOffTexture;
        MakeSound(UiSoundEnum.Click);
    }
    private void UiVisibility(UiInterfaceEnum _type)
    {
        if (_type != 0) { MakeSound(UiSoundEnum.Click); }
        curUi = _type;
        for (int i = 0; i < allUi.Count; i++) 
        {
            bool _show = (int)_type == i;
            allUi[i].alpha = _show ? 1f : 0f;
            allUi[i].interactable = _show;
            allUi[i].blocksRaycasts = _show;
        }
    }
    #endregion

    #region logic
    public void SetGrid(int _id) 
    {
        if (nextTurnUpdate || isGameOver) return;//Frequent operation or game is over,return
        //Not a space, invalid click
        if (boardData[_id % 3, _id / 3] != emptyData)
        {
            return;
        }
        //Effective
        MakeSound(UiSoundEnum.Piece);
        boardData[_id % 3, _id / 3] = isCircleMove ? circleData : crossData;
        grids[_id].texture = isCircleMove ? circleTexture : crossTexture;
        grids[_id].color = Color.white;
        NextTurn(!isCircleMove);
    }
    public void GameReset() 
    {
        isGameOver = false;
        result.alpha = 0f;
        result.blocksRaycasts = false;
        result.interactable = false;
        resultUpdate = false;
        boardData = new int[3, 3];
        for (int i = 0; i < grids.Count; i++)
        {
            grids[i].color = Color.clear;
        }
        NextTurn(isFirstCircleMove);
    }
    public void GameResetUi() 
    {
        MakeSound(UiSoundEnum.Reset);
        GameReset();
    }
    public void GameOver(int _type)
    {
        MakeSound(UiSoundEnum.Gameover);
        isGameOver = true;
        resultUpdate = true;
        resultTimer = 0f;
        string _txt;
        if (_type == circleData)
        { resultImage.color = new Color(circleColor.r, circleColor.g, circleColor.b, 0.98f); _txt = circleWinText; }
        else if (_type == crossData)
        { resultImage.color = new Color(crossColor.r, crossColor.g, crossColor.b, 0.98f); _txt = crossWinText; }
        else 
        { resultImage.color = new Color(0.5f, 0.5f, 0.5f, 0.98f); _txt = drawText; }

        foreach (var item in winText) 
        {
            item.text = _txt;
        }
    }
    public void NextTurn(bool _circle=true) 
    {
        //Is End
        if(CheckWin(boardData, isCircleMove ? circleData : crossData)) 
        { GameOver(isCircleMove ? circleData : crossData); return; }
        if (GetEmptyNumber(boardData) == 0) { GameOver(0);return;}
        //Next turn
        isCircleMove = _circle;
        nextTurnTimer = 0f;
        nextTurnUpdate = true;
        tempCircleColor = circleColor;
        tempCrossColor = crossColor;
    }
    private int GetEmptyNumber(int[,] board)
    {
        int empty = 0;
        for (int i = 0; i < 9; i++)
        {
            if (board[i % 3, i / 3] == emptyData)
            {
                empty += 1;
            }
        }
        return empty;
    }
    private bool CheckWin(int[,] board,int currentPlayer)
    {
        // Check rows and columns
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] == currentPlayer && board[i, 1] == currentPlayer && board[i, 2] == currentPlayer)
                return true;
            if (board[0, i] == currentPlayer && board[1, i] == currentPlayer && board[2, i] == currentPlayer)
                return true;
        }
        
        // Check diagonals
        if (board[0, 0] == currentPlayer && board[1, 1] == currentPlayer && board[2, 2] == currentPlayer)
            return true;
        if (board[0, 2] == currentPlayer && board[1, 1] == currentPlayer && board[2, 0] == currentPlayer)
            return true;

        return false;
    }
    #endregion

    #region sound
    public void MakeSound(UiSoundEnum _type)
    {
        if (!isSoundOn) return;
        switch (_type)
        {
            case UiSoundEnum.Click: audioSource.PlayOneShot(clickSound); break;
            case UiSoundEnum.Gameover: audioSource.PlayOneShot(gameoverSound); break;
            case UiSoundEnum.Reset: audioSource.PlayOneShot(resetSound); break;
            case UiSoundEnum.Piece: audioSource.PlayOneShot(pieceSound); break;
            default:
                break;
        }
    }
    #endregion

    #region ai
    private void AiAct() 
    {
        aiBestMove = 0;
        bool useRandomOperation = false;
        if (aiDifficulty == 0) { useRandomOperation = Random.Range(0f, 1f) < 0.35f; }
        else if (aiDifficulty == 1) { useRandomOperation = Random.Range(0f, 1f) < 0.75f; }
        if (useRandomOperation)
        {
            aiBestMove=AiRandomOperation(boardData);
        }
        else
        {
            remain = GetEmptyNumber(boardData);
            //In the first round, take random actions, avoid Alphabeta calculations
            if (remain == 9)
            {
                aiBestMove = Random.Range(0, 9);
            }
            //In the second round,act according to the preset strategy, avoid Alphabeta calculations
            else if (remain == 8)
            {
                int _op = 0;
                for (int i = 0; i < 9; i++)
                {
                    if (boardData[i % 3, i / 3] == circleData)
                    {
                        _op = i; break;
                    }
                }
                aiBestMove = presetStrategy[_op];
            }
            else
            {
                Alphabeta(boardData, int.MinValue, int.MaxValue, true, 0);
            }
        }

        if(boardData[aiBestMove % 3, aiBestMove / 3] != emptyData) 
        {
            aiBestMove = AiRandomOperation(boardData);
        }
        SetGrid(aiBestMove);
    }
    private int AiRandomOperation(int[,] board) 
    {
        allEmpty = new();
        for (int i = 0; i < 9; i++)
        {
            if (board[i % 3, i / 3] == emptyData)
            {
                allEmpty.Add(i);
            }
        }
        int _id;
        if (allEmpty.Count > 1)
        {
            _id = allEmpty[Random.Range(0, allEmpty.Count)];
        }
        else
        {
            _id = allEmpty[0];
        }
        return _id;
    }
    private int Alphabeta(int[,] board,int alpha,int beta,bool max,int depth) 
    {
        bool aiEnd = false; int score = max ? -8 : 8;
        if (CheckWin(board, crossData)) { score = 1; aiEnd = true; }
        else if (CheckWin(board, circleData)) { score = -1; aiEnd = true; }
        if (!aiEnd)
        { if (GetEmptyNumber(board)==0) { score = 0; aiEnd = true; } }
        if (aiEnd) return score;

        for (int i = 0; i < 9; i++)
        {
            int aiTempX = i % 3;
            int aiTempY = i / 3;
            if (board[aiTempX, aiTempY] == emptyData)
            {
                board[aiTempX, aiTempY] = max ? crossData : circleData;
                var tempScore = Alphabeta(board, alpha, beta, !max, depth+1);
                board[aiTempX, aiTempY] = emptyData;
                if (max)
                {
                    if (tempScore > score) 
                    {
                        score = tempScore;
                        if (depth == 0)
                        {
                            aiBestMove = i;
                        }
                    }
                }
                else 
                {
                    if (tempScore < score)
                    {
                        score = tempScore;
                    }
                }
                //Alpha-beta pruning
                if (max)
                {
                    alpha = Mathf.Max(alpha, score);
                }
                else
                {
                    beta = Mathf.Min(beta, score);
                }
                if (beta <= alpha) { break; }
            }
        }
        return score;
    }
    #endregion
}
