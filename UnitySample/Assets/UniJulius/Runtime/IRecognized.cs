using UnityEngine;

namespace UniJulius.Runtime
{
    public enum RecognitionType
    {
        Isolated,
        Grammar,
        FreePhrase,
        Spell,
    }
    
    public interface IRecognized
    {
        string JconfPath { get; }
        string DictPath { get; }
        RecognitionType RecognitionType { get; }
    }
}