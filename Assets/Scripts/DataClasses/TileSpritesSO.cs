using System.Linq;
using Pulsar.Interactions;
using UnityEditor;
using UnityEngine;

namespace Pulsar.DataClasses
{
[CreateAssetMenu(fileName = "NewSprites", menuName = "Pulsar/Tile Sprites")]
public class TileSpritesSO : ScriptableObject
{
    private const int k_Length = 5;
    private const int k_FirstSprite = 0;
    private const int k_SecondSprite = 1;
    private const int k_ThirdSprite = 2;
    private const int k_FourthSprite = 3;
    private const int k_FifthSprite = 4;

    [SerializeField]
    private Texture2D spriteSheet;

    [SerializeField]
    private Sprite[] frames = new Sprite[k_Length];

    public Sprite Get(HealthState state)
    {
        if (frames == null || frames.Length == 0)
        {
            return null;
        }
        
        int index = state switch
        {
            HealthState.FullHealth             => k_FirstSprite,
            HealthState.LessOrEqualTo75Percent => k_SecondSprite,
            HealthState.LessOrEqualTo50Percent => k_ThirdSprite,
            HealthState.LessOrEqualTo25Percent => k_FourthSprite,
            HealthState.LessOrEqualTo10Percent => k_FifthSprite,
            _ => k_FirstSprite,
        };

        // safety clamp in case there aren't enough sprites in the sheet 
        index = Mathf.Min(index, frames.Length - 1); 
        return frames[index] ? frames[index] : frames[k_FirstSprite];
    }
    
    // This auto gets the sliced frames and puts them in order in the list
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!spriteSheet)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(spriteSheet);
        
        Sprite[] sliced = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            // s.rect is the pixel rectangle that the frame takes up in its
            // spritesheet 
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();
 
        // to check if we split it up into frames yet or not, because if not
        // it would wipe the array 
        if (sliced.Length > 0)
        {
            frames = sliced;
        }
    }
#endif
}
}