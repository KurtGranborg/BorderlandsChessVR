/* Kurt Granborg 2017
 * Borderlands VR Chess Game
 * King.cs
 */
 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Chessman {

    public override bool[,] PossibleMove()
    {
        bool[,] r = new bool[8, 8];
        Chessman c;
        int i, j;
        i = CurrentX - 1;
        j = CurrentY + 1;
        
        if(CurrentY != 7)
        {
            for(int k = 0; k < 3; k++)
            {
                if(i >= 0 && i < 8)
                {
                    c = BoardManager.Instance.Chessmans[i, j];
                    if (c == null)
                        r[i, j] = true;
                    else if (isWhite != c.isWhite)
                        r[i, j] = true;
                }
                i++;
            }
        }
        i = CurrentX - 1;
        j = CurrentY - 1;
        if (CurrentY != 0)
        {
            for (int k = 0; k < 3; k++)
            {
                if (i >= 0 && i < 8)
                {
                    c = BoardManager.Instance.Chessmans[i, j];
                    if (c == null)
                        r[i, j] = true;
                    else if (isWhite != c.isWhite)
                        r[i, j] = true;
                }
                i++;
            }
        }

        if(CurrentX != 0)
        {
            c = BoardManager.Instance.Chessmans[CurrentX - 1, CurrentY];
            if (c == null)
                r[CurrentX - 1, CurrentY] = true;
            else if(isWhite != c.isWhite)
            {
                r[CurrentX - 1, CurrentY] = true;
            }
        }
        if (CurrentX != 7)
        {
            c = BoardManager.Instance.Chessmans[CurrentX + 1, CurrentY];
            if (c == null)
                r[CurrentX + 1, CurrentY] = true;
            else if (isWhite != c.isWhite)
            {
                r[CurrentX + 1, CurrentY] = true;
            }
        }
        if (BoardManager.Instance.isWhiteTurn) {
            if (BoardManager.Instance.WhiteCanCastle[0])
            {
                bool canCastle = true;
                for(int free = 4; free >= 0; free--)
                {
                    Chessman[,] Check = new Chessman[8, 8];
                    for (i = 0; i < 8; i++)
                    {
                        for (j = 0; j < 8; j++)
                            Check[i, j] = BoardManager.Instance.Chessmans[i, j];
                    }
                    Check[free, CurrentY] = Check[CurrentX, CurrentY];
                    if (free != 4)
                        Check[CurrentX, CurrentY] = null;
                    if (free > 1 && BoardManager.Instance.IsWhiteKingInCheck(Check))
                    {
                        canCastle = false;
                        break;
                    }
                    if (free != 0 && free != 4)
                    {
                        c = BoardManager.Instance.Chessmans[free, CurrentY];
                        if (c != null)
                            canCastle = false;
                    }
                }
                    r[CurrentX - 2, CurrentY] = canCastle;
            }
            if (BoardManager.Instance.WhiteCanCastle[1])
            {
                bool canCastle = true;
                for (int free = 4; free < 8; free++)
                {
                    Chessman[,] Check = new Chessman[8, 8];
                    for (i = 0; i < 8; i++)
                    {
                        for (j = 0; j < 8; j++)
                            Check[i, j] = BoardManager.Instance.Chessmans[i, j];
                    }
                    Check[free, CurrentY] = Check[CurrentX, CurrentY];
                    if (free != 4)
                        Check[CurrentX, CurrentY] = null;
                    if (free < 7 && BoardManager.Instance.IsWhiteKingInCheck(Check))
                    {
                        canCastle = false;
                        break;
                    }
                    if (free != 7 && free != 4)
                    {
                        c = BoardManager.Instance.Chessmans[free, CurrentY];
                        if (c != null)
                            canCastle = false;
                    }
                }
                r[CurrentX + 2, CurrentY] = canCastle;
            }
        }else
        {
            if (BoardManager.Instance.BlackCanCastle[0])
            {
                bool canCastle = true;
                for (int free = 4; free >= 0; free--)
                {
                    Chessman[,] Check = new Chessman[8, 8];
                    for(i = 0; i < 8; i++)
                    {
                        for (j = 0; j < 8; j++)
                        {
                            Check[i, j] = BoardManager.Instance.Chessmans[i, j];
                        }
                    }
                        Check[free, CurrentY] = Check[CurrentX, CurrentY];
                    if(free!=4)
                        Check[CurrentX, CurrentY] = null;
                    if (free > 1 && BoardManager.Instance.IsBlackKingInCheck(Check))
                    {
                        canCastle = false;
                        break;
                    }
                    if (free != 0 && free != 4)
                    {
                        c = BoardManager.Instance.Chessmans[free, CurrentY];
                        if (c != null)
                            canCastle = false;
                    }
                }
                r[CurrentX - 2, CurrentY] = canCastle;
            }
            if (BoardManager.Instance.BlackCanCastle[1])
            {
                bool canCastle = true;
                for (int free = 4; free < 8; free++)
                {
                    Chessman[,] Check = new Chessman[8, 8];
                    for (i = 0; i < 8; i++)
                    {
                        for (j = 0; j < 8; j++)
                            Check[i, j] = BoardManager.Instance.Chessmans[i, j];
                    }
                        Check[free, CurrentY] = Check[CurrentX, CurrentY];
                    if(free != 4)
                        Check[CurrentX, CurrentY] = null;
                    if (free < 7 && BoardManager.Instance.IsBlackKingInCheck(Check))
                    {
                        canCastle = false;
                        break;
                    }
                    if (free != 7 && free != 4)
                    {
                        c = BoardManager.Instance.Chessmans[free, CurrentY];
                        if (c != null)
                            canCastle = false;
                    }
                }
                r[CurrentX + 2, CurrentY] = canCastle;
            }
        }
        for (i = 0; i < 8; i++)
        {
            for (j = 0; j < 8; j++)
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
        return r;
    }
}
