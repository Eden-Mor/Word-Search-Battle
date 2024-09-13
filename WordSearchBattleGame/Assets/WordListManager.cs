using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class WordListManager : MonoBehaviour
{
    public GameObject textPrefab; // Assign the Text prefab in the Inspector

    private RectTransform m_rectTrans;
    private RectTransform rectTrans => m_rectTrans ??= GetComponent<RectTransform>();


    private List<string> wordList;


    public void PopulateList(List<string> strings)
    {
        // Clear existing children
        foreach (Transform child in rectTrans)
            Destroy(child.gameObject);

        wordList = strings;

        // Add new strings
        foreach (string str in strings)
        {
            GameObject newText = Instantiate(textPrefab, rectTrans);
            newText.GetComponent<TextMeshPro>().text = str;
        }
    }

    public void MarkWordCompleted(string word, string playerName)
    {
        int index = wordList.IndexOf(word);

        if (index != -1)
        {
            var gameObject = rectTrans.GetChild(index).gameObject;

            var tmp = gameObject.GetComponent<TextMeshPro>();

            tmp.color = Color.black;
            tmp.text = "(" + playerName + ") " + tmp.text;
        }
    }

}
