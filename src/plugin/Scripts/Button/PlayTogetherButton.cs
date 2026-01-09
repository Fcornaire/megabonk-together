using Assets.Scripts.Managers;
using UnityEngine;

namespace MegabonkTogether.Scripts.Button
{
    public class PlayTogetherButton : MyButtonNormal
    {
        private MainMenu mainMenu;

        public override void OnClick()
        {
            OnPlayTogetherClick();
        }


        private void OnPlayTogetherClick()
        {
            ButtonManager.selectedButton2 = this;

            var networkMenuObj = new GameObject("NetworkMenuTab");
            Plugin.Instance.NetworkTab = networkMenuObj.AddComponent<NetworkMenuTab>();
            Plugin.Instance.NetworkTab.SetMainMenu(mainMenu);
        }

        public void SetMainMenu(MainMenu menu)
        {
            mainMenu = menu;
        }
    }
}
