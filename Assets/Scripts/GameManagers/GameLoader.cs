using Pulsar.DataClasses;
using UnityEngine;

namespace Pulsar
{
/// <summary>
/// Simple helper script to handle initializing our main menu.
/// Lets us stop using string references by allowing levels to be AssetReferences,
/// but that requires us to use this workaround since AssetReferences can't be
/// stored in the Scene list.
/// </summary>
public class GameLoader : MonoBehaviour
{
    private async void Start()
    {
        await GameManager.Instance.LoadLevel(GameState.MainMenu);
    }
}
}
