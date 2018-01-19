/* Kurt Granborg 2017
 * Borderlands VR Chess Game
 * BoardHighlights.cs
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardHighlights : MonoBehaviour
{

    public static BoardHighlights Instance { set; get; }

    public GameObject highlightPrefab;
    public GameObject highlightCompPrefab;
    public GameObject highlightKingPrefab;


    private List<GameObject> highlights;
    private GameObject CompWas;
    private GameObject CompIs;
    private GameObject KingCheck;

    private void Start()
    {
        Instance = this;
        highlights = new List<GameObject>();
        CompWas = Instantiate(highlightCompPrefab);
        CompIs = Instantiate(highlightCompPrefab);
        KingCheck = Instantiate(highlightKingPrefab);
        CompWas.SetActive(false);
        CompIs.SetActive(false);
        KingCheck.SetActive(false);
    }

    private GameObject GetHighlightObject()
    {
        GameObject go = highlights.Find(g => !g.activeSelf);
        if (go == null)
        {
            go = Instantiate(highlightPrefab);
            highlights.Add(go);
        }
        return go;
    }
    public void HighlightCheckedKing(int x, int y)
    {
        KingCheck.transform.parent = BoardManager.Instance.plane.transform;
        KingCheck.SetActive(true);
        KingCheck.transform.localPosition = BoardManager.Instance.GetTileCenter(x, y);
    }
    public void UnhighlightKing()
    {
        KingCheck.SetActive(false);
    }
    public void HighlightAllowedMoves(bool[,] moves)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (moves[i, j])
                {
                    GameObject go = GetHighlightObject();
                    go.transform.parent = BoardManager.Instance.plane.transform;
                    go.SetActive(true);
                    go.transform.localPosition = BoardManager.Instance.GetTileCenter(i, j);
                }
            }
        }
    }
    public void HighlightCompMove(int wasX, int wasY, int isX, int isY)
    {
        CompWas.SetActive(true);
        CompIs.SetActive(true);
        CompWas.transform.parent = BoardManager.Instance.plane.transform;
        CompIs.transform.parent = BoardManager.Instance.plane.transform;
        CompWas.transform.localPosition = BoardManager.Instance.GetTileCenter(wasX, wasY);
        CompIs.transform.localPosition = BoardManager.Instance.GetTileCenter(isX, isY);
    }
    public void HideCompMove()
    {
        CompWas.SetActive(false);
        CompIs.SetActive(false);
    }
    public void HideHighlights()
    {
        foreach (GameObject go in highlights)
            go.SetActive(false);
    }
}