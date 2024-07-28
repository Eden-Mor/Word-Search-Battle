using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class SessionData
    {
        public SocketDataType DataType;
        public string Data;
    }

    [Serializable]
    public class WordItem
    {
        public string Word;
        public int StartX;
        public int StartY;
        public int EndX;
        public int EndY;
    }

    public enum SocketDataType
    {
        Error = 0,
        Start = 1,
        End = 2,
        WordCompleted = 3
    }
}
