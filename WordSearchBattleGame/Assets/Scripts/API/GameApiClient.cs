using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using System;

namespace Assets.Scripts.API
{
    public class GameApiClient
    {
        private readonly string _baseUrl;
        private string _authorizationHeader;

        public GameApiClient(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public void SetAuthorizationHeader(string scheme, string parameter) => _authorizationHeader = $"{scheme} {parameter}";

        public async Task<string> GetAsync(string endpoint)
        {
            try
            {
                var requestUrl = $"{_baseUrl}/{endpoint}";
                using UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl);

                if (!string.IsNullOrEmpty(_authorizationHeader))
                    webRequest.SetRequestHeader("Authorization", _authorizationHeader);

                var operation = webRequest.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {webRequest.error}");
                    return null;
                }

                return webRequest.downloadHandler.text;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        public async Task<string> PostAsync(string endpoint, string json)
        {
            var requestUrl = $"{_baseUrl}/{endpoint}";
            using UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(requestUrl, json);

            if (!string.IsNullOrEmpty(_authorizationHeader))
                webRequest.SetRequestHeader("Authorization", _authorizationHeader);

            webRequest.SetRequestHeader("Content-Type", "application/json");

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {webRequest.error}");
                return null;
            }

            return webRequest.downloadHandler.text;
        }

        public async Task<string> PutAsync(string endpoint, string json)
        {
            var requestUrl = $"{_baseUrl}/{endpoint}";
            using UnityWebRequest webRequest = UnityWebRequest.Put(requestUrl, json);

            if (!string.IsNullOrEmpty(_authorizationHeader))
                webRequest.SetRequestHeader("Authorization", _authorizationHeader);

            webRequest.SetRequestHeader("Content-Type", "application/json");

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {webRequest.error}");
                return null;
            }

            return webRequest.downloadHandler.text;
        }

        public async Task<string> DeleteAsync(string endpoint)
        {
            var requestUrl = $"{_baseUrl}/{endpoint}";
            using UnityWebRequest webRequest = UnityWebRequest.Delete(requestUrl);

            if (!string.IsNullOrEmpty(_authorizationHeader))
                webRequest.SetRequestHeader("Authorization", _authorizationHeader);

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {webRequest.error}");
                return null;
            }

            return webRequest.downloadHandler.text;
        }
    }
}
