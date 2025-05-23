using System;

namespace NaughtyAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class AnimatorStateAttribute : DrawerAttribute
    {
        public string AnimatorFieldName;

        public AnimatorStateAttribute(string animatorFieldName)
        {
            AnimatorFieldName = animatorFieldName;
        }
    }
}
