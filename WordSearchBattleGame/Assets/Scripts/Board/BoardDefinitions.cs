using System;

namespace WordSearchBattle.Scripts
{
    public abstract class UserActionEvents
    {
        public abstract event Action LoginToSocketClicked;
        public abstract event Action StartGameClicked;
    }

    public abstract class GameView
    {
        public abstract void AddPlayerJoinedText(string text);
        public abstract void StartGame();
        public abstract void GameLost();
        public abstract void GameTie();
        public abstract void GameWon();
    }
}