using Assets.Scripts.API;
using Assets.Scripts.Board;
using Assets.Scripts.GameData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WordSearchBattle.Scripts
{
    public class BoardComponent : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardEvents _boardEvents;
        [SerializeField] private GameApiService _gameAPI;
        [SerializeField] private GameDataObject _gameData;
        [SerializeField] private GridManager _gridManager;

        private GameBoardLogic _gameBoardLogic;

        private void Start()
        {
            if (_gameAPI == null)
            {
                Debug.LogError("GameApiService reference is not set in BoardComponent");
                return;
            }
        }

        public void Awake()
        {
            _boardView.Initialize(GetComponentsInChildren<BoardTileView>());

            _gameBoardLogic = new GameBoardLogic(_boardView, _boardEvents, _gameAPI, _gameData, _gridManager);
            _gameBoardLogic.Initialize();
        }

        private void OnDestroy()
        {
            _gameBoardLogic.DeInitialize();
        }

        public void OnStartGameClicked()
        {
            _boardEvents.OnStartGameClicked();
        }

        public void OnTileClicked(BoardTileView tileView)
        {
            _boardEvents.OnTileClicked(tileView);
        }
    }
}
