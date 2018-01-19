/* Kurt Granborg 2017
 * Borderlands VR Chess Game
 * BoardManager.cs
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;

public class BoardManager : MonoBehaviour
{
    //Uses the Stockfish 8 chess Engine for AI moves.    
    Process Stockfish;
    ProcessStartInfo stockfishStartInfo;
    StreamReader outputReader;
    StreamWriter inputWriter;
    enum comp { wasColumn, wasRank, isColumn, isRank };
    enum soundToPlay { loss, gameLoss, reset, take, gameWin };

    public Chessman[,] Chessmans { set; get; }
    public GameObject AngelMovie;
    public GameObject BlackPlayerPlane;
    public GameObject DifficultyText;
    public GameObject HyperionLogo;
    public GameObject LoseText;
    public GameObject music;
    public GameObject VaultHunterLogo;
    public GameObject WhitePlayerPlane;
    public GameObject WinText;
    public GameObject plane;
    public List<GameObject> Sounds;
    public List<GameObject> chessmenPrefabs;
    public NewtonVR.NVRButton ChangeTeamButton;
    public NewtonVR.NVRButton DifficultyDown;
    public NewtonVR.NVRButton DifficultyUp;
    public NewtonVR.NVRButton musicButton;
    public NewtonVR.NVRButton newGameButton;
    public NewtonVR.NVRButton resetButton;
    public NewtonVR.NVRHand left;
    public NewtonVR.NVRHand right;
    public bool BlackKingInCheck = false;
    public bool WhiteKingInCheck = false;
    public bool isWhiteTurn = true;
    public bool nextGamePlayerColorWhite = true;
    public bool playerTeamWhite = true;
    public bool[] BlackCanCastle { set; get; }
    public bool[] WhiteCanCastle { set; get; }
    public int BlackKingColumn = 4;
    public int BlackKingRank = 7;
    public int HalfMoves { set; get; }
    public int TotalMoves { set; get; }
    public int WhiteKingColumn = 4;
    public int WhiteKingRank = 0;
    public int[] EnPassantMove { set; get; }
    public static BoardManager Instance { set; get; }
    public string bitboard { set; get; }
    public string compMove { set; get; }

    private Chessman pieceInLeftHand;
    private Chessman pieceInRightHand;
    private Chessman selectedChessman;
    private List<GameObject> activeChessman;
    private Quaternion orientation = Quaternion.Euler(0, 90, 0);
    private bool endAudioPlayed = false;
    private bool[,] allowedMoves { set; get; }
    private float TILE_OFFSET;
    private float TILE_SIZE;
    private int difficulty = 5;
    private int numClips = 7;
    private int pieceLostAudio = 3;
    private int resetAudio = 1;
    private int selectionX = -1;
    private int selectionY = -1;
    private int takesPieceAudio = 1;
    private int winAudio = 2;
    private static string difficultyString = "setoption name Skill Level value ";
    


    private void Start()
    {
        WinText.SetActive(false);
        LoseText.SetActive(false);
        ((MovieTexture)AngelMovie.GetComponent<Renderer>().material.mainTexture).loop = true;
        ((MovieTexture)AngelMovie.GetComponent<Renderer>().material.mainTexture).Play();
        if (!playerTeamWhite)
        {
            VaultHunterLogo.SetActive(false);
            plane = BlackPlayerPlane.gameObject;
            orientation = Quaternion.Euler(0, -90, 0);
        }else
        {
            HyperionLogo.SetActive(false);
        }
        UnityEngine.Debug.Log(playerTeamWhite);
        TILE_SIZE = plane.GetComponent<BoxCollider>().bounds.size.z/8;

        TILE_OFFSET = TILE_SIZE / 2;
        stockfishStartInfo = new ProcessStartInfo("stockfish_8_x64.exe");
        stockfishStartInfo.UseShellExecute = false;
        stockfishStartInfo.ErrorDialog = false;
        stockfishStartInfo.RedirectStandardError = true;
        stockfishStartInfo.RedirectStandardInput = true;
        stockfishStartInfo.RedirectStandardOutput = true;
        stockfishStartInfo.CreateNoWindow = true;

        Stockfish = new Process();
        Stockfish.StartInfo = stockfishStartInfo;

        bool processStarted = Stockfish.Start();
        if (!processStarted)
            UnityEngine.Debug.Log("Stockfish Not Found, no AI!");
        else
        {
            inputWriter = Stockfish.StandardInput;
            outputReader = Stockfish.StandardOutput;
        }
        inputWriter.WriteLine();
        inputWriter.WriteLine("isready");
        inputWriter.WriteLine(" uci ");
        inputWriter.WriteLine(" ucinewgame ");
        Instance = this;
        TotalMoves = 0;
        HalfMoves = 0;
        SpawnAllChessmen();
        inputWriter.WriteLine("ucinewgame");
        UpdateBitboard();
        inputWriter.WriteLine(("position fen " + bitboard + "\n"));
        inputWriter.WriteLine("go");
        compMove = outputReader.ReadLine();
        //Filter contemplated moves.
        while (!compMove.Contains("bestmove"))
        {
            compMove = outputReader.ReadLine();
        }
        compMove = compMove.Replace("bestmove ", "");
        compMove = compMove.Remove(4, compMove.Length - 4);

    }
    private void Update()
    {
        CheckForGameOver();
        UpdateSelection();
        CheckButtonStates();
        SelectOrMovePiece();
    }

    //Pre: Chessmans should contain a valid boardstate
    //Post: bitboard string contains the current boardstate
    public void UpdateBitboard()
    {
        bitboard = "";
        for (int j = 7; j >= 0; j--)
        {
            int blanks = 0;
            for (int i = 0; i < 8; i++)
            {

                if (Chessmans[i, j] == null)
                {
                    blanks += 1;
                }
                else
                {

                    if (blanks > 0)
                        bitboard += blanks.ToString();
                    blanks = 0;
                }
                if (Chessmans[i, j] == null)
                {

                }
                else if (Chessmans[i, j].GetType() == typeof(Pawn))
                {
                    if (!Chessmans[i, j].isWhite)
                        bitboard += 'p';
                    else
                        bitboard += 'P';
                }
                else if (Chessmans[i, j].GetType() == typeof(Rook))
                {
                    if (!Chessmans[i, j].isWhite)
                        bitboard += 'r';
                    else
                        bitboard += 'R';
                }
                else if (Chessmans[i, j].GetType() == typeof(Knight))
                {
                    if (!Chessmans[i, j].isWhite)
                        bitboard += 'n';
                    else
                        bitboard += 'N';
                }
                else if (Chessmans[i, j].GetType() == typeof(Bishop))
                {
                    if (!Chessmans[i, j].isWhite)
                        bitboard += 'b';
                    else
                        bitboard += 'B';
                }
                else if (Chessmans[i, j].GetType() == typeof(Queen))
                {
                    if (!Chessmans[i, j].isWhite)
                        bitboard += 'q';
                    else
                        bitboard += 'Q';
                }
                else if (Chessmans[i, j].GetType() == typeof(King))
                {
                    if (!Chessmans[i, j].isWhite)
                        bitboard += 'k';
                    else
                        bitboard += 'K';
                }

            }
            if (blanks > 0)
                bitboard += blanks.ToString();
            bitboard += '/';
        }
        if (isWhiteTurn)
        {
            bitboard += " w";
        }
        else
        {
            bitboard += " b";
        }
        bitboard += " ";
        if (WhiteCanCastle[1])
        {
            bitboard += "K";
        }
        if (WhiteCanCastle[0])
        {
            bitboard += "Q";
        }
        if (BlackCanCastle[1])
        {
            bitboard += "k";
        }
        if (BlackCanCastle[0])
        {
            bitboard += "q";
        }
        if (!BlackCanCastle[0] && !BlackCanCastle[1] && !WhiteCanCastle[0] && !WhiteCanCastle[1])
        {
            bitboard += "-";
        }
        if (EnPassantMove[1] != -1)
        {
            bitboard += " " + (EnPassantMove[1] + 1).ToString();
        }
        else
        {
            bitboard += " -";
        }
        bitboard += " " + HalfMoves.ToString();
        bitboard += " " + TotalMoves.ToString();
    }
    //Pre: Array passed should contain a valid boardstate
    //Post: Returns true if white king is in check, false otherwise
    public bool IsWhiteKingInCheck(Chessman[,] Chessmans)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (Chessmans[i, j] != null && Chessmans[i, j].GetType() == typeof(King) && Chessmans[i, j].isWhite)
                {
                    WhiteKingColumn = i;
                    WhiteKingRank = j;
                }

            }
        }
        bool KingCheck;

        KingCheck = false;

        bool DangerFromRight = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingColumn + i) < 8) && (DangerFromRight == true) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank)] != null)
            {
                if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                  || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite))
                    DangerFromRight = false;
                else if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank)].isWhite))
                    DangerFromRight = false;
            }
        }


        bool DangerFromLeft = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingColumn - i) >= 0) && (DangerFromLeft == true) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank)] != null)
            {
                if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                  || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite))
                    DangerFromLeft = false;
                else if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank)].isWhite))
                    DangerFromLeft = false;
            }
        }


        bool DangerFromUp = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingRank + i) < 8) && (DangerFromUp == true) && Chessmans[(WhiteKingColumn), (WhiteKingRank + i)] != null)
            {
                if ((Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                  || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite))
                    DangerFromUp = false;
                else if ((Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn), (WhiteKingRank + i)].isWhite))
                    DangerFromUp = false;
            }
        }

        bool DangerFromDown = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingRank - i) >= 0) && (DangerFromDown == true) && Chessmans[(WhiteKingColumn), (WhiteKingRank - i)] != null)
            {
                if ((Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                  || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite))
                    DangerFromDown = false;
                else if ((Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn), (WhiteKingRank - i)].isWhite))
                    DangerFromDown = false;
            }
        }

        bool DangerFromUpLeft = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingColumn - i) >= 0) && ((WhiteKingRank + i) < 8) && (DangerFromUpLeft == true) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)] != null)
            {
                if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                  || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite))
                    DangerFromUpLeft = false;
                else if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite)
                       || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank + i)].isWhite))
                    DangerFromUpLeft = false;
            }
        }

        bool DangerFromUpRight = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingColumn + i) < 8) && ((WhiteKingRank + i) < 8) && (DangerFromUpRight == true) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)] != null)
            {
                if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                    || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                        || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                        || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                        || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                        || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite))
                    DangerFromUpRight = false;
                else if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                        || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                        || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite)
                        || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank + i)].isWhite))
                    DangerFromUpRight = false;
            }
        }

        bool DangerFromDownLeft = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingColumn - i) >= 0) && ((WhiteKingRank - i) >= 0) && (DangerFromDownLeft == true) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)] != null)
            {
                if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                    || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                        || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                        || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                        || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                        || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite))
                    DangerFromDownLeft = false;
                else if ((Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                        || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                        || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite)
                        || (Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn - i), (WhiteKingRank - i)].isWhite))
                    DangerFromDownLeft = false;
            }
        }

        bool DangerFromDownRight = true;

        for (int i = 1; i < 8; i++)
        {
            if (((WhiteKingColumn + i) < 8) && ((WhiteKingRank - i) >= 0) && (DangerFromDownRight == true) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)] != null)
            {
                if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Bishop) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                 || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Queen) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Pawn) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Rook) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Knight) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Bishop) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Queen) && Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite))
                    DangerFromDownRight = false;
                else if ((Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Pawn) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Knight) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(Rook) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite)
                       || (Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].GetType() == typeof(King) && !Chessmans[(WhiteKingColumn + i), (WhiteKingRank - i)].isWhite))
                    DangerFromDownRight = false;
            }

        }


        //CheckForPawns
        if (((WhiteKingColumn) < 7) && ((WhiteKingRank) > 0) && Chessmans[(WhiteKingColumn + 1), (WhiteKingRank + 1)] != null)
        {
            if ((Chessmans[(WhiteKingColumn + 1), (WhiteKingRank + 1)].GetType() == typeof(Pawn)) && !Chessmans[(WhiteKingColumn + 1), (WhiteKingRank + 1)].isWhite)
            {
                KingCheck = true;
            }
        }
        if (((WhiteKingColumn) > 0) && ((WhiteKingRank) > 0) && Chessmans[(WhiteKingColumn - 1), (WhiteKingRank + 1)] != null)
        {
            if ((Chessmans[(WhiteKingColumn - 1), (WhiteKingRank + 1)].GetType() == typeof(Pawn)) && !Chessmans[(WhiteKingColumn - 1), (WhiteKingRank + 1)].isWhite)
            {
                KingCheck = true;
            }
        }
        //Check for Knights
        if (((WhiteKingColumn + 1) < 8) && ((WhiteKingRank + 2) < 8) && Chessmans[WhiteKingColumn + 1, WhiteKingRank + 2] != null)
            if (Chessmans[WhiteKingColumn + 1, WhiteKingRank + 2].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn + 1, WhiteKingRank + 2].isWhite)
                KingCheck = true;
        if (((WhiteKingColumn + 1) < 8) && ((WhiteKingRank - 2) >= 0) && Chessmans[WhiteKingColumn + 1, WhiteKingRank - 2] != null)
            if (Chessmans[WhiteKingColumn + 1, WhiteKingRank - 2].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn + 1, WhiteKingRank - 2].isWhite)
                KingCheck = true;
        if (((WhiteKingColumn - 1) >= 0) && ((WhiteKingRank + 2) < 8) && Chessmans[WhiteKingColumn - 1, WhiteKingRank + 2] != null)
            if (Chessmans[WhiteKingColumn - 1, WhiteKingRank + 2].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn - 1, WhiteKingRank + 2].isWhite)
                KingCheck = true;
        if (((WhiteKingColumn - 1) >= 0) && ((WhiteKingRank - 2) >= 0) && Chessmans[WhiteKingColumn - 1, WhiteKingRank - 2] != null)
            if (Chessmans[WhiteKingColumn - 1, WhiteKingRank - 2].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn - 1, WhiteKingRank - 2].isWhite)
                KingCheck = true;

        if (((WhiteKingColumn + 2) < 8) && ((WhiteKingRank + 1) < 8) && Chessmans[WhiteKingColumn + 2, WhiteKingRank + 1] != null)
            if (Chessmans[WhiteKingColumn + 2, WhiteKingRank + 1].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn + 2, WhiteKingRank + 1].isWhite)
                KingCheck = true;
        if (((WhiteKingColumn + 2) < 8) && ((WhiteKingRank - 1) >= 0) && Chessmans[WhiteKingColumn + 2, WhiteKingRank - 1] != null)
            if (Chessmans[WhiteKingColumn + 2, WhiteKingRank - 1].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn + 2, WhiteKingRank - 1].isWhite)
                KingCheck = true;
        if (((WhiteKingColumn - 2) >= 0) && ((WhiteKingRank + 1) < 8) && Chessmans[WhiteKingColumn - 2, WhiteKingRank + 1] != null)
            if (Chessmans[WhiteKingColumn - 2, WhiteKingRank + 1].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn - 2, WhiteKingRank + 1].isWhite)
                KingCheck = true;
        if (((WhiteKingColumn - 2) >= 0) && ((WhiteKingRank - 1) >= 0) && Chessmans[WhiteKingColumn - 2, WhiteKingRank - 1] != null)
            if (Chessmans[WhiteKingColumn - 2, WhiteKingRank - 1].GetType() == typeof(Knight) && !Chessmans[WhiteKingColumn - 2, WhiteKingRank - 1].isWhite)
                KingCheck = true;
        return KingCheck;

    }
    //Pre: Array passed should contain a valid boardstate
    //Post: Returns true if black king is in check, false otherwise
    public bool IsBlackKingInCheck(Chessman[,] Chessmans)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (Chessmans[i, j] != null && Chessmans[i, j].GetType() == typeof(King) && !Chessmans[i, j].isWhite)
                {
                    BlackKingColumn = i;
                    BlackKingRank = j;
                }

            }
        }
        bool KingCheck;

        KingCheck = false;

        bool DangerFromRight = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingColumn + i) < 8) && (DangerFromRight == true) && Chessmans[(BlackKingColumn + i), (BlackKingRank)] != null)
            {
                if ((Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                  || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite))
                    DangerFromRight = false;
                else if ((Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank)].GetType() == typeof(King) && Chessmans[(BlackKingColumn + i), (BlackKingRank)].isWhite))
                    DangerFromRight = false;
            }
        }


        bool DangerFromLeft = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingColumn - i) >= 0) && (DangerFromLeft == true) && Chessmans[(BlackKingColumn - i), (BlackKingRank)] != null)
            {
                if ((Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                  || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite))
                    DangerFromLeft = false;
                else if ((Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank)].GetType() == typeof(King) && Chessmans[(BlackKingColumn - i), (BlackKingRank)].isWhite))
                    DangerFromLeft = false;
            }
        }


        bool DangerFromUp = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingRank + i) < 8) && (DangerFromUp == true) && Chessmans[(BlackKingColumn), (BlackKingRank + i)] != null)
            {
                if ((Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                  || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite))
                    DangerFromUp = false;
                else if ((Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank + i)].GetType() == typeof(King) && Chessmans[(BlackKingColumn), (BlackKingRank + i)].isWhite))
                    DangerFromUp = false;
            }
        }

        bool DangerFromDown = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingRank - i) >= 0) && (DangerFromDown == true) && Chessmans[(BlackKingColumn), (BlackKingRank - i)] != null)
            {
                if ((Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                  || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite))
                    DangerFromDown = false;
                else if ((Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn), (BlackKingRank - i)].GetType() == typeof(King) && Chessmans[(BlackKingColumn), (BlackKingRank - i)].isWhite))
                    DangerFromDown = false;
            }
        }

        bool DangerFromUpLeft = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingColumn - i) >= 0) && ((BlackKingRank + i) < 8) && (DangerFromUpLeft == true) && Chessmans[(BlackKingColumn - i), (BlackKingRank + i)] != null)
            {
                if ((Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                  || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite))
                    DangerFromUpLeft = false;
                else if ((Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite)
                       || (Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].GetType() == typeof(King) && Chessmans[(BlackKingColumn - i), (BlackKingRank + i)].isWhite))
                    DangerFromUpLeft = false;
            }
        }

        bool DangerFromUpRight = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingColumn + i) < 8) && ((BlackKingRank + i) < 8) && (DangerFromUpRight == true) && Chessmans[(BlackKingColumn + i), (BlackKingRank + i)] != null)
            {
                if ((Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                    || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                        || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                        || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                        || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                        || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite))
                    DangerFromUpRight = false;
                else if ((Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                        || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                        || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite)
                        || (Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].GetType() == typeof(King) && Chessmans[(BlackKingColumn + i), (BlackKingRank + i)].isWhite))
                    DangerFromUpRight = false;
            }
        }

        bool DangerFromDownLeft = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingColumn - i) >= 0) && ((BlackKingRank - i) >= 0) && (DangerFromDownLeft == true) && Chessmans[(BlackKingColumn - i), (BlackKingRank - i)] != null)
            {
                if ((Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                    || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                        || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                        || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                        || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                        || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite))
                    DangerFromDownLeft = false;
                else if ((Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                        || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                        || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite)
                        || (Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].GetType() == typeof(King) && Chessmans[(BlackKingColumn - i), (BlackKingRank - i)].isWhite))
                    DangerFromDownLeft = false;
            }
        }

        bool DangerFromDownRight = true;

        for (int i = 1; i < 8; i++)
        {
            if (((BlackKingColumn + i) < 8) && ((BlackKingRank - i) >= 0) && (DangerFromDownRight == true) && Chessmans[(BlackKingColumn + i), (BlackKingRank - i)] != null)
            {
                if ((Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Bishop) && Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                 || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Queen) && Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite))
                    KingCheck = true;
                else if ((Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Pawn) && !Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Rook) && !Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Knight) && !Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Bishop) && !Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Queen) && !Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite))
                    DangerFromDownRight = false;
                else if ((Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Pawn) && Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Knight) && Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(Rook) && Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite)
                       || (Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].GetType() == typeof(King) && Chessmans[(BlackKingColumn + i), (BlackKingRank - i)].isWhite))
                    DangerFromDownRight = false;
            }

        }


        //CheckForPawns
        if (((BlackKingColumn) < 7) && ((BlackKingRank) > 0) && Chessmans[(BlackKingColumn + 1), (BlackKingRank - 1)] != null)
        {
            if ((Chessmans[(BlackKingColumn + 1), (BlackKingRank - 1)].GetType() == typeof(Pawn)) && Chessmans[(BlackKingColumn + 1), (BlackKingRank - 1)].isWhite)
            {
                KingCheck = true;
            }
        }
        if (((BlackKingColumn) > 0) && ((BlackKingRank) > 0) && Chessmans[(BlackKingColumn - 1), (BlackKingRank - 1)] != null)
        {
            if ((Chessmans[(BlackKingColumn - 1), (BlackKingRank - 1)].GetType() == typeof(Pawn)) && Chessmans[(BlackKingColumn - 1), (BlackKingRank - 1)].isWhite)
            {
                KingCheck = true;
            }
        }
        //Check for Knights
        if (((BlackKingColumn + 1) < 8) && ((BlackKingRank + 2) < 8) && Chessmans[BlackKingColumn + 1, BlackKingRank + 2] != null)
            if (Chessmans[BlackKingColumn + 1, BlackKingRank + 2].GetType() == typeof(Knight) && Chessmans[BlackKingColumn + 1, BlackKingRank + 2].isWhite)
                KingCheck = true;
        if (((BlackKingColumn + 1) < 8) && ((BlackKingRank - 2) >= 0) && Chessmans[BlackKingColumn + 1, BlackKingRank - 2] != null)
            if (Chessmans[BlackKingColumn + 1, BlackKingRank - 2].GetType() == typeof(Knight) && Chessmans[BlackKingColumn + 1, BlackKingRank - 2].isWhite)
                KingCheck = true;
        if (((BlackKingColumn - 1) >= 0) && ((BlackKingRank + 2) < 8) && Chessmans[BlackKingColumn - 1, BlackKingRank + 2] != null)
            if (Chessmans[BlackKingColumn - 1, BlackKingRank + 2].GetType() == typeof(Knight) && Chessmans[BlackKingColumn - 1, BlackKingRank + 2].isWhite)
                KingCheck = true;
        if (((BlackKingColumn - 1) >= 0) && ((BlackKingRank - 2) >= 0) && Chessmans[BlackKingColumn - 1, BlackKingRank - 2] != null)
            if (Chessmans[BlackKingColumn - 1, BlackKingRank - 2].GetType() == typeof(Knight) && Chessmans[BlackKingColumn - 1, BlackKingRank - 2].isWhite)
                KingCheck = true;

        if (((BlackKingColumn + 2) < 8) && ((BlackKingRank + 1) < 8) && Chessmans[BlackKingColumn + 2, BlackKingRank + 1] != null)
            if (Chessmans[BlackKingColumn + 2, BlackKingRank + 1].GetType() == typeof(Knight) && Chessmans[BlackKingColumn + 2, BlackKingRank + 1].isWhite)
                KingCheck = true;
        if (((BlackKingColumn + 2) < 8) && ((BlackKingRank - 1) >= 0) && Chessmans[BlackKingColumn + 2, BlackKingRank - 1] != null)
            if (Chessmans[BlackKingColumn + 2, BlackKingRank - 1].GetType() == typeof(Knight) && Chessmans[BlackKingColumn + 2, BlackKingRank - 1].isWhite)
                KingCheck = true;
        if (((BlackKingColumn - 2) >= 0) && ((BlackKingRank + 1) < 8) && Chessmans[BlackKingColumn - 2, BlackKingRank + 1] != null)
            if (Chessmans[BlackKingColumn - 2, BlackKingRank + 1].GetType() == typeof(Knight) && Chessmans[BlackKingColumn - 2, BlackKingRank + 1].isWhite)
                KingCheck = true;
        if (((BlackKingColumn - 2) >= 0) && ((BlackKingRank - 1) >= 0) && Chessmans[BlackKingColumn - 2, BlackKingRank - 1] != null)
            if (Chessmans[BlackKingColumn - 2, BlackKingRank - 1].GetType() == typeof(Knight) && Chessmans[BlackKingColumn - 2, BlackKingRank - 1].isWhite)
                KingCheck = true;

        return KingCheck;

    }
    //Pre: 
    //Post: Returns the local coordinate of the center of tile x, y, on plane board object
    public Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = plane.transform.TransformVector(Vector3.zero);
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.y += .001f;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;
        return origin;
    }

    private int CheckForBlackMate()
    {
        if (HalfMoves >= 50)
            return -1;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (Chessmans[i, j] != null && !Chessmans[i, j].isWhite)
                {
                    bool[,] moves = Chessmans[i, j].PossibleMove();
                    for (int k = 0; k < 8; k++)
                        for (int l = 0; l < 8; l++)
                            if (moves[k, l])
                            {
                                return 0;
                            }
                }
            }
        if (IsBlackKingInCheck(Chessmans))
            return 1;
        return -1;
    }
    private int CheckForWhiteMate()
    {
        if (HalfMoves >= 50)
            return -1;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (Chessmans[i, j] != null && Chessmans[i, j].isWhite)
                {
                    bool[,] moves = Chessmans[i, j].PossibleMove();
                    for (int k = 0; k < 8; k++)
                        for (int l = 0; l < 8; l++)
                            if (moves[k, l])
                            {
                                return 0;
                            }
                }
            }
        if (IsWhiteKingInCheck(Chessmans))
            return 1;
        return -1;
    }
    private int getCompMove(char pos)
    {
        switch (pos)
        {
            case '1':
                return 0;
            case '2':
                return 1;
            case '3':
                return 2;
            case '4':
                return 3;
            case '5':
                return 4;
            case '6':
                return 5;
            case '7':
                return 6;
            case '8':
                return 7;
            case 'a':
                return 0;
            case 'b':
                return 1;
            case 'c':
                return 2;
            case 'd':
                return 3;
            case 'e':
                return 4;
            case 'f':
                return 5;
            case 'g':
                return 6;
            case 'h':
                return 7;

        }
        return -1;

    }
    private void CheckButtonStates()
    {
        if (resetButton.ButtonDown == true)
        {
            PlayAudio(soundToPlay.reset);
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    if (Chessmans[i, j] != null)
                    {
                        Chessmans[i, j].transform.parent = plane.transform;
                        Chessmans[i, j].transform.eulerAngles = Chessmans[i, j].Orientation;
                        Chessmans[i, j].transform.localPosition = (GetTileCenter(i, j));
                        Chessmans[i, j].GetComponent<Rigidbody>().velocity = Vector3.zero;
                        Chessmans[i, j].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

                    }
                }
        }
        if (musicButton.ButtonDown == true)
        {
            if (music.GetComponent<AudioSource>().isPlaying)
                music.GetComponent<AudioSource>().Pause();
            else
                music.GetComponent<AudioSource>().UnPause();
        }
        if (newGameButton.ButtonDown == true)
        {
            EndGame();
        }
        if (ChangeTeamButton.ButtonDown == true)
        {
            nextGamePlayerColorWhite = !nextGamePlayerColorWhite;
            HyperionLogo.SetActive(!HyperionLogo.activeSelf);
            VaultHunterLogo.SetActive(!VaultHunterLogo.activeSelf);
        }
        if (DifficultyDown.ButtonDown == true)
        {
            if (difficulty > 0)
            {
                difficulty -= 1;
                DifficultyText.GetComponent<TextMesh>().text = difficulty.ToString().PadLeft(2, '0');
                inputWriter.WriteLine(difficultyString + difficulty.ToString());
            }
        }
        if (DifficultyUp.ButtonDown == true)
        {
            if (difficulty < 20)
            {
                difficulty += 1;
                DifficultyText.GetComponent<TextMesh>().text = difficulty.ToString().PadLeft(2, '0');
                inputWriter.WriteLine(difficultyString + difficulty.ToString());
            }
        }
    }
    private void CheckForGameOver()
    {
        int whiteMate = CheckForWhiteMate();
        int blackMate = CheckForBlackMate();
        if (whiteMate == 1)
        {
            if (!playerTeamWhite)
            {
                WinText.SetActive(true);
                if (!endAudioPlayed)
                    PlayAudio(soundToPlay.gameLoss);
                endAudioPlayed = true;
            }
            else
            {
                LoseText.SetActive(true);
                if (!endAudioPlayed)
                    PlayAudio(soundToPlay.gameWin);
                endAudioPlayed = true;
            }
        }
        else if (blackMate == 1)
        {
            if (playerTeamWhite)
            {
                WinText.SetActive(true);
                if (!endAudioPlayed)
                    PlayAudio(soundToPlay.gameLoss);
                endAudioPlayed = true;
            }
            else
            {
                LoseText.SetActive(true);
                if (!endAudioPlayed)
                    PlayAudio(soundToPlay.gameWin);
                endAudioPlayed = true;
            }
        }
        else if (blackMate == -1 || whiteMate == -1)
        {
            UnityEngine.Debug.Log("Draw!");
        }
        if (playerTeamWhite)
        {
            if (IsWhiteKingInCheck(Chessmans))
            {
                BoardHighlights.Instance.HighlightCheckedKing(WhiteKingColumn, WhiteKingRank);
            }
            else
            {
                BoardHighlights.Instance.UnhighlightKing();
            }
        }
        else
        {
            if (IsBlackKingInCheck(Chessmans))
            {
                BoardHighlights.Instance.HighlightCheckedKing(BlackKingColumn, BlackKingRank);
            }
            else
            {
                BoardHighlights.Instance.UnhighlightKing();
            }
        }
    }
    private void PlayAudio(soundToPlay Audio)
    {
        for (int i = 0; i < numClips; i++)
        {
            if (Sounds[i].GetComponent<AudioSource>().isPlaying)
                return;
        }
        System.Random rand = new System.Random();
        int random = 0;
        switch (Audio)
        {
            case soundToPlay.loss:
                random = rand.Next(pieceLostAudio + 3);
                if (random < pieceLostAudio)
                {
                    Sounds[random].GetComponent<AudioSource>().Play();
                }
                break;
            case soundToPlay.gameLoss:
                random = rand.Next(pieceLostAudio);
                Sounds[random].GetComponent<AudioSource>().Play();
                break;
            case soundToPlay.reset:
                random = rand.Next(resetAudio+2);
                if (random < resetAudio)
                {
                    random += pieceLostAudio;
                    Sounds[random].GetComponent<AudioSource>().Play();
                }
                break;
            case soundToPlay.take:
                random = rand.Next(takesPieceAudio + 10);
                if (random < takesPieceAudio)
                {
                    random += pieceLostAudio + resetAudio;
                    Sounds[random].GetComponent<AudioSource>().Play();
                }
                break;
            case soundToPlay.gameWin:
                random = rand.Next(winAudio);
                random += pieceLostAudio + resetAudio + takesPieceAudio;
                Sounds[random].GetComponent<AudioSource>().Play();
                break;
            default:
                return;
        }
    }
    private void SelectOrMovePiece()
    {
        if (isWhiteTurn == playerTeamWhite)
        {
            if (selectionX >= 0 && selectionY >= 0)
            {
                if (selectedChessman != null
                    && selectedChessman.transform.localPosition.y < 1
                    && !(selectionX == selectedChessman.CurrentX
                    && selectionY == selectedChessman.CurrentY)
                    && !(right.IsInteracting && (left.CurrentlyInteracting is Chessman))
                    && !(left.IsInteracting && (left.CurrentlyInteracting is Chessman)))
                {
                    MoveChessman(selectionX, selectionY);
                }
                else
                {

                    SelectChessman(selectionX, selectionY);
                }
            }
        }
        else
        {
            BoardHighlights.Instance.HighlightCompMove(getCompMove(compMove[0]), getCompMove(compMove[1]), getCompMove(compMove[2]), getCompMove(compMove[3]));
            SelectChessman(getCompMove(compMove[(int)comp.wasColumn]), getCompMove(compMove[(int)comp.wasRank]));
            MoveChessman(getCompMove(compMove[(int)comp.isColumn]), getCompMove(compMove[(int)comp.isRank]));
            BoardHighlights.Instance.HideHighlights();
        }
    }
    private void SpawnChessman(int index, int x, int y)
    {
        GameObject go = Instantiate(chessmenPrefabs[index], GetTileCenter(x, y), orientation) as GameObject;
        go.transform.parent = plane.transform;
        go.transform.localPosition = GetTileCenter(x, y);
        Chessmans[x, y] = go.GetComponent<Chessman>();
        Chessmans[x, y].SetPosition(x, y);
        Chessmans[x, y].SetOrientation(Chessmans[x, y].transform.eulerAngles);
        activeChessman.Add(go);
    }
    private void SpawnAllChessmen()
    {
        activeChessman = new List<GameObject>();
        Chessmans = new Chessman[8, 8];
        EnPassantMove = new int[2] { -1, -1 };
        BlackCanCastle = new bool[2] { true, true };
        WhiteCanCastle = new bool[2] { true, true };
        //Spawn Kings
        SpawnChessman(0, 4, 0);
        SpawnChessman(6, 4, 7);
        //Spawn Queens
        SpawnChessman(1, 3, 0);
        SpawnChessman(7, 3, 7);
        //Spawn Rooks
        SpawnChessman(2, 0, 0);
        SpawnChessman(2, 7, 0);
        SpawnChessman(8, 0, 7);
        SpawnChessman(8, 7, 7);
        //Spawn Bishops
        SpawnChessman(3, 2, 0);
        SpawnChessman(3, 5, 0);
        SpawnChessman(9, 2, 7);
        SpawnChessman(9, 5, 7);
        //Spawn Knights
        SpawnChessman(4, 1, 0);
        SpawnChessman(4, 6, 0);
        SpawnChessman(10, 1, 7);
        SpawnChessman(10, 6, 7);
        //Spawn Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(5, i, 1);
            SpawnChessman(11, i, 6);
        }


    }
    private void SelectChessman(int x, int y)
    {
        if (Chessmans[x, y] == null)
            return;
        if (Chessmans[x, y].isWhite != isWhiteTurn)
            return;
        bool hasAtLeastOneMove = false;
        //Check allowed moves, only select if moves are possible. 
        allowedMoves = Chessmans[x, y].PossibleMove();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
                if (allowedMoves[i, j])
                    hasAtLeastOneMove = true;
        }
        if (hasAtLeastOneMove)
            selectedChessman = Chessmans[x, y];
        BoardHighlights.Instance.HighlightAllowedMoves(allowedMoves);
    }
    private void UpdateSelection()
    {
        if (selectedChessman != null && selectedChessman.transform.localPosition.y < 1)
        {
            selectionX = (int)(selectedChessman.transform.localPosition.x / TILE_SIZE);
            selectionY = (int)(selectedChessman.transform.localPosition.z / TILE_SIZE);
            if (!(selectionX >= 0 && selectionX < 8 && selectionY >= 0 && selectionY < 8))
            {
                selectionX = -1;
                selectionY = -1;
            }
        }
        if (right.IsInteracting && right.CurrentlyInteracting is Chessman)
        {
            BoardHighlights.Instance.HideHighlights();
            pieceInRightHand = right.CurrentlyInteracting as Chessman;
            if (pieceInRightHand.isWhite == playerTeamWhite)
            {
                selectionX = pieceInRightHand.CurrentX;
                selectionY = pieceInRightHand.CurrentY;
            }
        }
        if (left.IsInteracting && left.CurrentlyInteracting is Chessman)
        {
            BoardHighlights.Instance.HideHighlights();
            pieceInRightHand = left.CurrentlyInteracting as Chessman;
            if (pieceInRightHand.isWhite == playerTeamWhite)
            {
                selectionX = pieceInRightHand.CurrentX;
                selectionY = pieceInRightHand.CurrentY;
            }
        }
    }
    private void MoveChessman(int x, int y)
    {
        if (allowedMoves[x, y])
        {
            BoardManager Temp = Instance;
            Chessman c = Chessmans[x, y];
            //Delete enemy chessman if applicable
            if (c != null && c.isWhite != isWhiteTurn)
            {
                HalfMoves = 0;
                activeChessman.Remove(c.gameObject);
                Destroy(c.gameObject);
                if (isWhiteTurn == playerTeamWhite)
                    PlayAudio(soundToPlay.loss);
                else
                    PlayAudio(soundToPlay.take);
            }
            //Correctly remove pawn if moving EnPassant
            if (x == EnPassantMove[0] && y == EnPassantMove[1] && selectedChessman.GetType() == typeof(Pawn))
            {
                if (isWhiteTurn)
                {
                    c = Chessmans[x, y - 1];
                    activeChessman.Remove(c.gameObject);
                    Destroy(c.gameObject);
                }
                else
                {
                    c = Chessmans[x, y + 1];
                    activeChessman.Remove(c.gameObject);
                    Destroy(c.gameObject);
                }
            }
            EnPassantMove[0] = -1;
            EnPassantMove[1] = -1;
            if (selectedChessman.GetType() == typeof(Pawn))
            {
                HalfMoves = 0;
                if (y == 7 || y == 0)
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    if (isWhiteTurn)
                        SpawnChessman(1, x, y);
                    else
                        SpawnChessman(7, x, y);
                    selectedChessman = Chessmans[x, y];
                }
                if (selectedChessman.CurrentY == 1 && y == 3)
                {
                    EnPassantMove[0] = x;
                    EnPassantMove[1] = y - 1;
                }
                else if (selectedChessman.CurrentY == 6 && y == 4)
                {
                    EnPassantMove[0] = x;
                    EnPassantMove[1] = y + 1;
                }
            }
            if (selectedChessman.GetType() == typeof(King))
            {
                if (isWhiteTurn)
                {
                    WhiteKingColumn = x;
                    WhiteKingRank = y;
                    WhiteCanCastle[0] = false;
                    WhiteCanCastle[1] = false;
                }
                else
                {
                    BlackKingColumn = x;
                    BlackKingRank = y;
                    BlackCanCastle[0] = false;
                    BlackCanCastle[1] = false;
                }

            }
            if (selectedChessman.GetType() == typeof(Rook))
            {
                if (isWhiteTurn)
                {
                    if (x == 0)
                        WhiteCanCastle[0] = false;
                    else
                        WhiteCanCastle[1] = false;
                }
                else
                {
                    if (x == 0)
                        BlackCanCastle[0] = false;
                    else
                        BlackCanCastle[1] = false;
                }

            }
            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            //Move pieces correctly if Castling.
            if (selectedChessman.GetType() == typeof(King) && (Mathf.Abs(selectedChessman.CurrentX - x) > 1))
            {

                if (isWhiteTurn)
                {
                    if (x == 2)
                    {
                        c = Chessmans[0, selectedChessman.CurrentY];
                        activeChessman.Remove(c.gameObject);
                        Destroy(c.gameObject);
                        SpawnChessman(2, selectedChessman.CurrentX - 1, selectedChessman.CurrentY);
                    }
                    else
                    {
                        c = Chessmans[7, selectedChessman.CurrentY];
                        activeChessman.Remove(c.gameObject);
                        Destroy(c.gameObject);
                        SpawnChessman(2, selectedChessman.CurrentX + 1, selectedChessman.CurrentY);
                    }
                }
                else
                {
                    if (x == 2)
                    {
                        c = Chessmans[x - 2, y];
                        activeChessman.Remove(c.gameObject);
                        Destroy(c.gameObject);
                        SpawnChessman(8, selectedChessman.CurrentX - 1, selectedChessman.CurrentY);
                    }
                    else
                    {
                        c = Chessmans[x + 1, y];
                        activeChessman.Remove(c.gameObject);
                        Destroy(c.gameObject);
                        SpawnChessman(8, selectedChessman.CurrentX + 1, selectedChessman.CurrentY);
                    }


                }

            }
            //Update piece position.
            selectedChessman.transform.localPosition = (GetTileCenter(x, y));
            selectedChessman.SetPosition(x, y);
            selectedChessman.transform.parent = plane.transform;
            selectedChessman.transform.eulerAngles = selectedChessman.Orientation;
            selectedChessman.transform.localPosition = (GetTileCenter(x, y));
            selectedChessman.GetComponent<Rigidbody>().velocity = Vector3.zero;
            selectedChessman.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            Chessmans[x, y] = selectedChessman;
            if (!isWhiteTurn)
            {
                TotalMoves += 1;
            }
            selectedChessman = null;
            selectionX = -1;
            selectionY = -1;
            BoardHighlights.Instance.HideHighlights();
            isWhiteTurn = !isWhiteTurn;

            HalfMoves += 1;
            //Create bitboard to send to Stockfish AI
            UpdateBitboard();
            inputWriter.WriteLine(("position fen " + bitboard + "\n"));
            inputWriter.WriteLine("go");
            compMove = outputReader.ReadLine();
            //Filter contemplated moves.
            while (!compMove.Contains("bestmove"))
            {
                if (outputReader.EndOfStream)
                {
                    inputWriter.WriteLine(("position fen " + bitboard + "\n"));
                    inputWriter.WriteLine("go");
                }
                compMove = outputReader.ReadLine();
            }
            compMove = compMove.Replace("bestmove ", "");
            compMove = compMove.Remove(4, compMove.Length - 4);
        }
        else
        {
            selectedChessman = null;
        }


    }
    private void EndGame()
    {
        endAudioPlayed = false;
        if (isWhiteTurn)
            UnityEngine.Debug.Log("White Team Wins!");
        else
            UnityEngine.Debug.Log("Black Team Wins!");
        foreach (GameObject go in activeChessman)
            Destroy(go);
        isWhiteTurn = true;
        playerTeamWhite = nextGamePlayerColorWhite;
        if (playerTeamWhite)
        {
            plane = WhitePlayerPlane;
            orientation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            plane = BlackPlayerPlane;
            orientation = Quaternion.Euler(0, -90, 0);
        }
        BoardHighlights.Instance.HideCompMove();
        BoardHighlights.Instance.UnhighlightKing();
        BoardHighlights.Instance.HideHighlights();
        WinText.SetActive(false);
        LoseText.SetActive(false);
        SpawnAllChessmen();
        UpdateBitboard();
        inputWriter.WriteLine("ucinewgame");
        UpdateBitboard();
        inputWriter.WriteLine(("position fen " + bitboard + "\n"));
        inputWriter.WriteLine("go");
        compMove = outputReader.ReadLine();
        //Filter contemplated moves.
        while (!compMove.Contains("bestmove"))
        {
            compMove = outputReader.ReadLine();
        }
        compMove = compMove.Replace("bestmove ", "");
        compMove = compMove.Remove(4, compMove.Length - 4);
    }
}
