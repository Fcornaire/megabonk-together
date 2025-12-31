using UnityEngine;
using UnityEngine.EventSystems;

namespace MegabonkTogether.Scripts.Button
{
    public class PlayTogetherButton : MyButtonNormal
    {
        private bool WasSelected = false;
        private MainMenu mainMenu;

        public override void OnClick()
        {
            OnPlayTogetherClick();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            WasSelected = true;
        }

        private void OnPlayTogetherClick()
        {
            var networkMenuObj = new GameObject("NetworkMenuTab");
            Plugin.Instance.NetworkTab = networkMenuObj.AddComponent<NetworkMenuTab>();
            Plugin.Instance.NetworkTab.SetMainMenu(mainMenu);
        }

        public bool HasBeenSelected()
        {
            return WasSelected;
        }

        public void ResetSelected()
        {
            WasSelected = false;
        }

        public void SetMainMenu(MainMenu menu)
        {
            mainMenu = menu;
        }
    }
}
