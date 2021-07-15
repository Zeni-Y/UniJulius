namespace UniJulius.Runtime
{
    /// <summary>
    /// Pass1:第1パスの計算結果
    /// Pass1Error:第1パスの計算中にエラーが発生
    /// Pass2:第2パスの計算結果
    /// Pass2Error:第2パスの計算中にエラーが発生
    /// </summary>
    public enum ResultType
    {
        Pass1, Pass2, Pass1Error, Pass2Error
    }

    public class RecognitionResult
    {
        public ResultType Type { get; private set; }
        public string SrInstanceName { get; private set; }
        public string GrammarName { get; private set; }
        public string Word { get; private set; }
        public int WordId { get; private set; }
        public float ConfidenceScore { get; private set; }
        public float LmScore { get; private set; }
        public float AmScore { get; private set; }

        public RecognitionResult(
            ResultType type,
            string srInstanceName,
            string grammarName,
            string word,
            int wordId,
            float confidenceScore,
            float lmScore,
            float amScore)
        {
            Type = type;
            SrInstanceName = srInstanceName;
            GrammarName = grammarName;
            Word = word;
            WordId = wordId;
            ConfidenceScore = confidenceScore;
            LmScore = lmScore;
            AmScore = amScore;
        }

        public RecognitionResult(
            ResultType type,
            string srInstanceName,
            string grammarName
            )
        {
            Type = type;
            SrInstanceName = srInstanceName;
            GrammarName = grammarName;
        }

        /// <summary>
        /// TO-DO:string builderなりで最適化する
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = "Result Type\t:" + Type.ToString() + "\n" +
                         "SR Instance Name\t:" + SrInstanceName + "\n" +
                         "Grammar Name\t:" + GrammarName + "\n" +
                         "Word\t:" + Word + "\n" +
                         "Word Id\t:" + WordId + "\n" +
                         "Confidence Score\t:" + ConfidenceScore + "\n" +
                         "LM Score\t:" + LmScore + "\n" +
                         "AM Score\t:" + AmScore + "\n"; 
            return result;
        }
    }
}