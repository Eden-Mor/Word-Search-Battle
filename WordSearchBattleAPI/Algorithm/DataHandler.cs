/*==========================================================================================*
*  Manage DataWords.xml word lists.                                                         *
*===========================================================================================*
*  1. ROUTINE: Handle return, process, word list from DataWords.xml                         *
*   a. Return STRING of specified list of Words from DataWords.xml                          *
*   b. Return string with all characters except a-z, removed                                * 
*   c. Return array of seperated STRING Words from given STRING                             *
*   d. Return STRING array of randomly chosen Words in list according to user input size    *
*  2. Return list of all word lists from DataWords.xml                                      *
*===========================================================================================*/
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace WordSearchBattleAPI.Algorithm
{
    public static class DataHandler
    {
        private const string WORD_LIST_LOCATION = "Resources/DataWords.xml";


        /*==========================================*
        *  1. Handle get, modify, return word list  *
        *===========================================*/
        public static string[] HandleListLoad(string nameList, int sizeList)
        {
            string wordsString = LoadListWords(nameList);
            string wordsStringSanitised = SanitiseWords(wordsString);
            string[] wordsSeperated = SeperateWords(wordsStringSanitised);
            string[] wordsSelected = SetSizeList(wordsSeperated, sizeList);
            string[] words = Helper.CapitaliseWordsAll(wordsSelected);

            return words;
        }

        public static List<string> GetThemeList()
        {
            using var xmlStream = new FileStream(WORD_LIST_LOCATION, FileMode.Open);

            XmlDocument doc = new();
            doc.Load(xmlStream);
            var nodes = doc.ChildNodes[1]?.ChildNodes;

            if (nodes == null)
                return [];

            var themes = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
                themes.Add(nodes[i]?.Name ?? string.Empty);

            return themes;
        }


        private static string LoadListWords(string nameListWords)
        {
            using var xmlStream = new FileStream(WORD_LIST_LOCATION, FileMode.Open);

            XmlDocument doc = new();
            doc.Load(xmlStream);

            var elements = doc.GetElementsByTagName(nameListWords);

            if (elements.Count <= 0)
                return string.Empty;

            string? list = elements.Item(0)?.InnerText;

            return list ?? string.Empty;
        }
        private static string SanitiseWords(string listWords)
        {
            string wordsSanitised = Regex.Replace(listWords, "[^A-Za-z ]", "");

            return wordsSanitised;
        }
        private static string[] SeperateWords(string listWords)
        {
            char[] delimiters = new char[] { ',', ' ', ';' };

            string[] words = listWords.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            return words;
        }
        private static string[] SetSizeList(string[] arrayWords, int sizeList)
        {
            bool wordAdded = false;
            List<string> listWords = new List<string>();

            for (int counter = 0; counter < sizeList; counter++)
            {
                wordAdded = false;
                while (!wordAdded)
                {
                    // Select word from random integer
                    string wordSelected = arrayWords[Helper.Random(0, arrayWords.Length - 1)];
                    // if word not already added to listWords, add
                    if (!listWords.Contains(wordSelected))
                    {
                        listWords.Add(wordSelected);
                        wordAdded = true;
                    }
                }
            }
            return listWords.ToArray();
        }

        /*==========================*
        *  2. Return all word lists *
        *===========================*/
        //public static string[] AllLists()
        //{
        //    XElement dataWords = XElement.Parse(Properties.Resources.DataWords);
        //    var xElements = dataWords.Elements();
        //    List<string> listOfLists = new List<string>();

        //    foreach (var item in xElements)
        //    {
        //        listOfLists.Add(item.Name.ToString());
        //    }
        //    string[] lists = listOfLists.ToArray();

        //    return lists;
        //}
    }
}