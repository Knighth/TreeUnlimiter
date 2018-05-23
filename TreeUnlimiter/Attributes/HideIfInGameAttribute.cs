using TreeUnlimiter.OptionsFramework.Attibutes;

namespace TreeUnlimiter
{
    public class HideIfInGameAttribute : HideConditionAttribute
    {
        public override bool IsHidden()
        {
            return LoadingExtension.InGame;
        }
    }
}