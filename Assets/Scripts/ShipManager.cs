using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class ShipManager : MonoBehaviour
{
    [SerializeField]
    private CompositeCollider2D compositeCollider;

    [SerializeField]
    private Rigidbody2D rigidBody; // [CB] Created with CompositeCollider

    [SerializeField]
    private List<Tile> tiles = new(20);

    [SerializeField]
    private List<Weapon> weapons = new(10);

    [SerializeField]
    private List<Thruster> thrusters = new(10);

    // [CB] Commenting out so Unity doesn't show warnings. We can uncomment as we need them.
    // public event Action<Tile> onNewTileAdded;
    // public event Action<Weapon> onNewWeaponAdded;
    // public event Action<Thruster> onNewThrusterAdded;

    private void Start()
    {
        tiles = gameObject.GetComponentsInChildren<Tile>().ToList();
        weapons = tiles.OfType<Weapon>().ToList();
        thrusters = tiles.OfType<Thruster>().ToList();
        UpdateWeaponColliders();
    }

    public void Fire()
    {
        foreach (Weapon weapon in weapons)
        {
            weapon.Fire();
        }
    }

    public void Move(Vector2 position)
    {
        // TODO: Add logic to figure out which thrusters to fire, and by how much
        foreach (Thruster thruster in thrusters)
        {
            //! [CB] PLACEHOLDER LOGIC
            thruster.Move(position.x, rigidBody);
        }
    }

    public void Rotate(float rotateForce)
    {
        //TODO: Add logic for rotating
        Debug.Log("Manager moving");
    }

    private void UpdateWeaponColliders()
    {
        foreach (Weapon weapon in weapons)
        {
            weapon.SetParentCollider(compositeCollider);
        }
    }
}