using Microsoft.AspNetCore.Mvc;
using System.Text;
using WordSearchBattleAPI.Algorithm;
using WordSearchBattleAPI.Models;

namespace WordSearchBattleAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WordSearchController : ControllerBase
    {

        private readonly ILogger<WordSearchController> _logger;

        public WordSearchController(ILogger<WordSearchController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetRandomWordSearch")]
        public Tuple<string[], string> Get()
        {
            Tuple<string[], char[,]> tuple = SetupGame();
            return new(tuple.Item1, ConvertCharArrayToStringGrid(tuple.Item2));
        }

        private static Tuple<string[], char[,]> SetupGame(int sizeList = 8, string nameList = "i")
        {
            WordSearch wordSearch = new();
            wordSearch.HandleSetupWords(nameList, sizeList);
            wordSearch.HandleSetupGrid();

            return new(wordSearch.Words, wordSearch.Grid);
        }

        public static string ConvertCharArrayToStringGrid(char[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sb.Append(array[i, j]);
                }

                if (i != rows - 1)
                    sb.Append(" \n "); // Add a new line after each row
            }

            return sb.ToString();
        }
    }
}
