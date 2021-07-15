using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniJulius.Runtime
{
    [Serializable]
    public class CommentBlockData
    {
        public List<string> ChildNodes = new List<string>();
        public Vector2 Position;
        public string Title = "Comment Block";
    }
}