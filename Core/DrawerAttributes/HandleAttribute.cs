using UnityEngine;

namespace NaughtyAttributes
{
    public class HandleAttribute : PropertyAttribute
    {
        public HandleType Type { get; }

        public HandleAttribute(HandleType type = HandleType.Local)
        {
            Type = type;
        }
    }
}