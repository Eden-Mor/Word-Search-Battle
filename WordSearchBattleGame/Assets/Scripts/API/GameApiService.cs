using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.GameData;
using Assets.Scripts.Models;
using UnityEngine.Windows;
using System;

namespace Assets.Scripts.API
{
    public class GameApiService : MonoBehaviour
    {
        private GameApiClient _apiClient;
        [SerializeField] private GameDataObject _gameData;

        private void Awake()
        {
            _apiClient = new GameApiClient("https://wordsearchbattle.api.edenmor.com");
            //_apiClient.SetAuthorizationHeader("Bearer", "your-access-token");

            //StartCoroutine(GetPlayerDataCoroutine("playerId123"));
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
