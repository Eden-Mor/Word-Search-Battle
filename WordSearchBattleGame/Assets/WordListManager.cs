using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class WordListManager : MonoBehaviour
{
    public GameObject textPrefab; // Assign the Text prefab in the Inspector

    private RectTransform rectTrans;

    void Awake() 
        => rectTrans = this.GetComponent<RectTransform>();

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
        // Find the index of the word in the list
        int index = wordList.IndexOf(word);

        if (index != -1)
        {
            //// Remove the word from the list
            //wordList.RemoveAt(index);

            // Destroy the corresponding GameObject
            var gameObject = rectTrans.GetChild(index).gameObject;

            var tmp = gameObject.GetComponent<TextMeshPro>();

            tmp.color = Color.black;
            tmp.text = "(" + playerName + ") " + tmp.text;

            wordList.RemoveAt(index);

            //// Re-populate the list to update the UI (optional, but ensures correct order)
            //PopulateList(wordList);
        }
    }

}
