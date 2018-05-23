using TreeUnlimiter.OptionsFramework.Attibutes;

namespace TreeUnlimiter
{
    public class HideIfNotInGameAttrubute : HideConditionAttribute
    {
        public override bool IsHidden()
        {
            return !LoadingExtension.InGame;
        }
    }
}