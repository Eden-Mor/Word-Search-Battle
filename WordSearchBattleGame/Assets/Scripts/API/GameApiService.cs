using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.GameData;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.API
{
    public class GameApiService : MonoBehaviour
    {
        private GameApiClient _apiClient;
        [SerializeField] private GameDataObject _gameData;

        private void Awake()
        {
            _apiClient = new GameApiClient($"https://{Constants.SERVER_URI}");

            //_apiClient.SetAuthorizationHeader("Bearer", "your-access-token");
            //StartCoroutine(GetPlayerDataCoroutine("playerId123"));
        }

        public void PopulatePublicServerList()
        {
            if (_apiClient == null)
                return;

            StartCoroutine(GetPublicServerList());
        }

        private IEnumerator GetPublicServerList()
        {
            Task<string> task = _apiClient.GetAsync("wordsearch/getpublicgames");
            yield return new WaitUntil(() => task.IsCompleted);

            Dictionary<string, int> serverList = new();

            bool resultOk = task.Result != null;

            if (resultOk)
                serverList = JsonUtility.FromJson<Dictionary<string, int>>(task.Result);

            Debug.Log($"Public games data: {task.Result}");
            _gameData.ServerListChanged(resultOk, serverList);
        }

        private IEnumerator GetPlayerDataCoroutine(string playerId)
        {
            Task<string> task = _apiClient.GetAsync($"players/{playerId}");
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result != null)
                Debug.Log($"Player Data: {task.Result}");
        }

        public IEnumerator GetRandomWordSearchCoroutine(Action<string> onComplete)
        {
            Task<string> task = _apiClient.GetAsync("wordsearch/getrandomwordsearch");
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted || task.Result == null)
                yield break;

            Debug.Log($"Player Data: {task.Result}");
            onComplete?.Invoke(task.Result);
        }

    }

}
