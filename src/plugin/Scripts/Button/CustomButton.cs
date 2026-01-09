using System;

namespace MegabonkTogether.Scripts.Button
{
    /// <summary>
    /// CustomButton that use a System.Action instead of Unity Action since they are broken right now with BepInex and/or Il2Cpp
    /// </summary>
    public class CustomButton : MyButtonNormal
    {
        private Action onClickAction;
        private Action onStartHover;
        private Action onEndHover;

        public void SetOnClickAction(Action action)
        {
            onClickAction = action;
        }

        public void OverrideStartHoverAction(Action action)
        {
            onStartHover = action;
        }

        public void OverrideEndHoverAction(Action action)
        {
            onEndHover = action;
        }

        public override void OnClick()
        {
            onClickAction?.Invoke();
        }

        public override void StartHover()
        {
            onStartHover?.Invoke();
        }

        public override void StopHover()
        {
            onEndHover?.Invoke();
        }

    }
}
