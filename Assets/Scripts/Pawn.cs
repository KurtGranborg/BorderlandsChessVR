/* Kurt Granborg 2017
 * Borderlands VR Chess Game
 * Pawn.cs
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Chessman
{
    public override bool[,] PossibleMove()
    {
        bool[,] r = new bool[8,8];
        Chessman c, c2;
        int[] e = BoardManager.Instance.EnPassantMove;
        bool EPUL = false, EPUR = false, EPDL = false, EPDR = false;


        if (isWhite)
        {
            //Diagonal Left
            if(CurrentX!=0 && CurrentY != 7)
            {
                if (e[0] == CurrentX - 1 && e[1] == CurrentY + 1)
                {
                    r[CurrentX - 1, CurrentY + 1] = true;
                    EPUL = true;
                }

                c = BoardManager.Instance.Chessmans[CurrentX - 1, CurrentY + 1];
                if (c != null && !c.isWhite)
                    r[CurrentX - 1, CurrentY + 1] = true;
            }

            //Diagonal Right
            if (CurrentX != 7 && CurrentY != 7)
            {
                if (e[0] == CurrentX + 1 && e[1] == CurrentY + 1)
                {
                    r[CurrentX + 1, CurrentY + 1] = true;
                    EPUR = true;
                }
                c = BoardManager.Instance.Chessmans[CurrentX + 1, CurrentY + 1];
                if (c != null && !c.isWhite)
                    r[CurrentX + 1, CurrentY + 1] = true;
            }
            //Middle
            if(CurrentY != 7)
            {
                c = BoardManager.Instance.Chessmans[CurrentX, CurrentY + 1];
                if (c == null)
                {
                    r[CurrentX, CurrentY + 1] = true;
                    if (CurrentY == 1)
                    {
                        c2 = BoardManager.Instance.Chessmans[CurrentX, CurrentY + 2];
                        if (c2 == null)
                            r[CurrentX, CurrentY + 2] = true;
                    }
                }
            }
           

        }else
        {
            //Diagonal Left
            if (CurrentX != 0 && CurrentY != 0)
            {
                if (e[0] == CurrentX - 1 && e[1] == CurrentY - 1)
                {
                    r[CurrentX - 1, CurrentY - 1] = true;
                    EPDL = true;
                }
                c = BoardManager.Instance.Chessmans[CurrentX - 1, CurrentY - 1];
                if (c != null && c.isWhite)
                    r[CurrentX - 1, CurrentY - 1] = true;
            }

            //Diagonal Right
            if (CurrentX != 7 && CurrentY != 0)
            {
                if (e[0] == CurrentX + 1 && e[1] == CurrentY - 1)
                {
                    r[CurrentX + 1, CurrentY - 1] = true;
                    EPDR = true;
                }
                c = BoardManager.Instance.Chessmans[CurrentX + 1, CurrentY - 1];
                if (c != null && c.isWhite)
                    r[CurrentX + 1, CurrentY - 1] = true;
            }
            //Middle
            if (CurrentY != 0)
            {
                c = BoardManager.Instance.Chessmans[CurrentX, CurrentY - 1];
                if (c == null)
                {
                    r[CurrentX, CurrentY - 1] = true;
                    if (CurrentY == 6)
                    {
                        c2 = BoardManager.Instance.Chessmans[CurrentX, CurrentY - 2];
                        if (c2 == null)
                            r[CurrentX, CurrentY - 2] = true;
                    }
                }
            }
        }
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (r[i, j])
                {
                    Chessman[,] Check = new Chessman[8, 8];
                    for (int k = 0; k < 8; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            Check[k, l] = BoardManager.Instance.Chessmans[k, l];
                        }
                    }
                    Check[i, j] = BoardManager.Instance.Chessmans[CurrentX, CurrentY];
                    Check[CurrentX, CurrentY] = null;

                    if (BoardManager.Instance.isWhiteTurn)
                    {
                        r[i, j] = !BoardManager.Instance.IsWhiteKingInCheck(Check);
                    }
                    else
                    {
                        r[i, j] = !BoardManager.Instance.IsBlackKingInCheck(Check);
                    }

                }
            }
        }
        if (EPUR)
        {
            Chessman[,] Check = new Chessman[8, 8];
            for (int k = 0; k < 8; k++)
            {
                for (int l = 0; l < 8; l++)
                {
                    Check[k, l] = BoardManager.Instance.Chessmans[k, l];
                }
            }
            Check[CurrentX + 1, CurrentY + 1] = Check[CurrentX, CurrentY];
            Check[CurrentX + 1, CurrentY] = null;
            r[CurrentX + 1, CurrentY + 1] = !BoardManager.Instance.IsBlackKingInCheck(Check);
        }
        if (EPUL)
        {
            Chessman[,] Check = new Chessman[8, 8];
            for (int k = 0; k < 8; k++)
            {
                for (int l = 0; l < 8; l++)
                {
                    Check[k, l] = BoardManager.Instance.Chessmans[k, l];
                }
            }
            Check[CurrentX - 1, CurrentY + 1] = Check[CurrentX, CurrentY];
            Check[CurrentX - 1, CurrentY] = null;
            r[CurrentX - 1, CurrentY + 1] = !BoardManager.Instance.IsWhiteKingInCheck(Check);
        }
        if (EPDR)
        {
            Chessman[,] Check = new Chessman[8, 8];
            for (int k = 0; k < 8; k++)
            {
                for (int l = 0; l < 8; l++)
                {
                    Check[k, l] = BoardManager.Instance.Chessmans[k, l];
                }
            }
            Check[CurrentX + 1, CurrentY - 1] = Check[CurrentX, CurrentY];
            Check[CurrentX + 1, CurrentY] = null;
            r[CurrentX + 1, CurrentY - 1] = !BoardManager.Instance.IsWhiteKingInCheck(Check);
        }
        if (EPDL)
        {
            Chessman[,] Check = new Chessman[8, 8];
            for (int k = 0; k < 8; k++)
            {
                for (int l = 0; l < 8; l++)
                {
                    Check[k, l] = BoardManager.Instance.Chessmans[k, l];
                }
            }
            Check[CurrentX - 1, CurrentY - 1] = Check[CurrentX, CurrentY];
            Check[CurrentX - 1, CurrentY] = null;
            r[CurrentX - 1, CurrentY - 1] = !BoardManager.Instance.IsBlackKingInCheck(Check);
        }
        return r;
    }
}
