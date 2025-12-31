using UnityEngine;

namespace MegabonkTogether.Scripts.Modal
{
    /// <summary>
    /// Simple loading modal
    /// </summary>
    internal class LoadingModal : ModalBase
    {
        protected override void OnUICreated()
        {
            ShowLoader();
        }

        public void UpdateMessage(string message)
        {
            SetStatusText(message);
        }

        public void Close()
        {
            CloseModal();
        }

        public static LoadingModal Show(string message)
        {
            var go = new GameObject("LoadingModal");
            GameObject.DontDestroyOnLoad(go);

            var modal = go.AddComponent<LoadingModal>();
            modal.UpdateMessage(message);

            return modal;
        }
    }
}
