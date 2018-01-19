/* Kurt Granborg 2017
 * Borderlands VR Chess Game
 * Chessman.cs
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Chessman : NewtonVR.NVRInteractableItem
{

	public int CurrentX { set; get; }
    public int CurrentY { set; get; }
    public Vector3 Orientation { set; get; }
    public bool isWhite;
    public void SetOrientation(Vector3 set)
    {
        Orientation = set;
    }
    public void SetPosition(int x, int y)
    {
        CurrentX = x;
        CurrentY = y; 
    }
    public virtual bool[,] PossibleMove()
    {
        return new bool[8, 8];
    }
}
