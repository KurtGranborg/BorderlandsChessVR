/* Kurt Granborg 2017
 * Borderlands VR Chess Game
 * Rook.cs
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : Chessman
{
public override bool[,] PossibleMove()
    {
        bool[,] r = new bool[8,8];
        Chessman c;
        int i = CurrentX;
        while (true)
        {
            i++;
                if (i >= 8)
                    break;
            c = BoardManager.Instance.Chessmans[i, CurrentY];
            if (c == null)
                r[i, CurrentY] = true;
            else
            {
                if (c.isWhite != isWhite)
                    r[i, CurrentY] = true;
                break;
            }
        }
        i = CurrentX;
        while (true)
        {
            i--;
            if (i < 0)
                break;
            c = BoardManager.Instance.Chessmans[i, CurrentY];
            if (c == null)
                r[i, CurrentY] = true;
            else
            {
                if (c.isWhite != isWhite)
                    r[i, CurrentY] = true;
                break;
            }
        }
        i = CurrentY;
        while (true)
        {
            i++;
            if (i >= 8)
                break;
            c = BoardManager.Instance.Chessmans[CurrentX, i];
            if (c == null)
                r[CurrentX, i] = true;
            else
            {
                if (c.isWhite != isWhite)
                    r[CurrentX, i] = true;
                break;
            }
        }
        i = CurrentY;
        while (true)
        {
            i--;
            if (i < 0)
                break;
            c = BoardManager.Instance.Chessmans[CurrentX, i];
            if (c == null)
                r[CurrentX, i] = true;
            else
            {
                if (c.isWhite != isWhite)
                    r[CurrentX, i] = true;
                break;
            }
        }
        for (i = 0; i < 8; i++)
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
        return r;
    }
}
