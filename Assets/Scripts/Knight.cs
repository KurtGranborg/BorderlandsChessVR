/* Kurt Granborg 2017
 * Borderlands VR Chess Game
 * Knight.cs
 */
 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : Chessman
{
    public override bool[,] PossibleMove()
    {
        bool[,] r = new bool[8, 8];
        KnightMove(CurrentX - 1, CurrentY + 2, ref r);
        KnightMove(CurrentX + 1, CurrentY + 2, ref r);
        KnightMove(CurrentX + 2, CurrentY + 1, ref r);
        KnightMove(CurrentX + 2, CurrentY - 1, ref r);
        KnightMove(CurrentX - 1, CurrentY - 2, ref r);
        KnightMove(CurrentX + 1, CurrentY - 2, ref r);
        KnightMove(CurrentX - 2, CurrentY + 1, ref r);
        KnightMove(CurrentX - 2, CurrentY - 1, ref r);
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
        return r;
    }


    public void KnightMove (int x, int y, ref bool[,] r)
    {
        Chessman c;
        if(x >= 0 && x < 8 && y >= 0 && y < 8)
        {
            c = BoardManager.Instance.Chessmans[x, y];
            if (c == null)
                r[x, y] = true;
            else if (isWhite != c.isWhite)
                r[x, y] = true;
        }
    }
   
}
