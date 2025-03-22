using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class PlayerCounter : MonoBehaviour
    {
        public static int PlayerCount;

        public void OnPlayerJoined()
        {
            PlayerCount++;
        }

        public void OnPlayerLeft()
        {
            PlayerCount--;
        }
    }
}
