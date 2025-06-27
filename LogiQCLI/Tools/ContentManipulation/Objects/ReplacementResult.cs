using System.Collections.Generic;

namespace LogiQCLI.Tools.ContentManipulation.Objects
{
    public class ReplacementResult
    {
        public bool Success { get; set; }
        public string ModifiedContent { get; set; } = string.Empty;
        public int ReplacementCount { get; set; }
        public List<int> MatchPositions { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;

        public static ReplacementResult CreateSuccess(string content, int count, List<int> positions)
        {
            return new ReplacementResult
            {
                Success = true,
                ModifiedContent = content,
                ReplacementCount = count,
                MatchPositions = positions
            };
        }

        public static ReplacementResult CreateFailure(string error)
        {
            return new ReplacementResult
            {
                Success = false,
                ErrorMessage = error,
                MatchPositions = new List<int>()
            };
        }
    }
}
