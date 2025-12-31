using System;

namespace MegabonkTogether.Scripts.Button
{
    /// <summary>
    /// CustomButton that use a System.Action instead of Unity Action since they are broken right now with BepInex and/or Il2Cpp
    /// </summary>
    public class CustomButton : MyButtonNormal
    {
        private Action onClickAction;

        public void SetOnClickAction(Action action)
        {
            onClickAction = action;
        }

        public override void OnClick()
        {
            onClickAction?.Invoke();
        }
    }
}
