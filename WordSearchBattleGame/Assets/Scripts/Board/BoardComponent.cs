using Assets.Scripts.API;
using Assets.Scripts.Board;
using Assets.Scripts.GameData;
using System.Drawing;
using UnityEngine;
using WordSearchBattleShared.API;

namespace WordSearchBattle.Scripts
{
    public class BoardComponent : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardEvents _boardEvents;
        [SerializeField] private GameApiService _gameAPI;
        [SerializeField] private GameDataObject _gameData;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private WordListManager _wordListManager;
        [SerializeField] private GameClient _GameClient;
        [SerializeField] private HighlightManager _highlightManager;
        [SerializeField] private ColorPickerManager _colorPickerManager;
        [SerializeField] private GameObject _debugLogIn;

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
#if UNITY_EDITOR
            _debugLogIn.SetActive(true);
#endif

            _boardView.Initialize();
            _gameBoardLogic = new GameBoardLogic(_boardView, _boardEvents, _gameAPI, _gameData, _gridManager, _wordListManager, _GameClient, _highlightManager, _colorPickerManager);
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

        public void OnFullScreenClicked()
        {
            WebGLSupport.WebGLWindow.SwitchFullscreen();
        }

        public void OnLoginClicked(bool local)
        {
            _boardEvents.OnLoginToSocketClicked(local);
        }

        public void OnColorClicked(KnownColor color)
        {
            _boardEvents.OnColorPicked(color);
        }
    }
}
