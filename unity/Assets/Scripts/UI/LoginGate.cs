using System.Collections;
using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// On Play, if the player has no activePet selected, show the Character Select screen.
    /// One-time gate — once a character is picked, this never fires again.
    /// </summary>
    public class LoginGate : MonoBehaviour
    {
        private void Start() => StartCoroutine(CheckOnNextFrame());

        private IEnumerator CheckOnNextFrame()
        {
            yield return null;
            yield return null;
            var data = Sparq.Core.SaveService.Data;
            if (data == null) yield break;
            if (string.IsNullOrEmpty(data.activePet))
            {
                CharacterSelect.Show();
            }
        }
    }
}
