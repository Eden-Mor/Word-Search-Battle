namespace Assets.Helpers
{
    public static class GridArrayHelper
    {
        public static char[,] ConvertToCharArray(string input, char separator)
        {
            var sections = input.Split(separator);
            int numRows = sections.Length;
            int numCols = sections[0].Length;

            char[,] charArray = new char[numRows, numCols];

            for (int i = 0; i < numRows; i++)
                for (int j = 0; j < numCols; j++)
                    charArray[i, j] = sections[i][j];

            return charArray;
        }
    }
}