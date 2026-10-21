using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class BackgroundScroll : MonoBehaviour
{
    
    //Different layered background pieces, to allow for astroids to render
    //separately in one script
    [SerializeField]
    private GameObject[] backgroundPieces;

    [SerializeField]
    private int bufferSize = 1;

    private Camera cam;

    
    private class LayerData
    {
        public Transform Container;
        public float TotalWidth;
        public float TotalHeight;
    }
    
    private List<LayerData> layers = new List<LayerData>();
    
    private void Start()
    {
        cam = Camera.main;
        Vector2 screenBounds = cam.ScreenToWorldPoint(
            new Vector3(Screen.width, Screen.height,
                cam.transform.position.z));
        foreach (GameObject obj in backgroundPieces)
        {
            LoadBackgroundPiece(obj, screenBounds);
        }
    }

    private void LoadBackgroundPiece(GameObject obj, Vector2 screenBounds)
    {
        Vector2 childSize = obj.GetComponent<SpriteRenderer>().bounds.size;
        float zPos = obj.transform.position.z;
        
        //How much space the child is going to take up once made, + buffer
        int childNeedsX = Mathf.CeilToInt(screenBounds.x * 2 / childSize.x) + bufferSize;
        int childNeedsY = Mathf.CeilToInt(screenBounds.y * 2 / childSize.y) + bufferSize;
        
        GameObject layerContainer = new GameObject(obj.name + "_Layer");
        layerContainer.transform.SetParent(this.transform);

        for (int i = -childNeedsX; i <= childNeedsX; i++)
        {
            for (int j = -childNeedsY; j <= childNeedsY; j++)
            {
                Vector3 position = new Vector3(
                    cam.transform.position.x + (childSize.x * i),
                    cam.transform.position.y + (childSize.y * j),
                    zPos);
                Instantiate(obj, position, Quaternion.identity, 
                    layerContainer.transform);
            }
        }

        //Stores each layer data once to avoid repeated calls
        layers.Add(new LayerData()
        {
            Container = layerContainer.transform,
            TotalWidth = childSize.x * ((childNeedsX * 2) + bufferSize),
            TotalHeight = childSize.y * ((childNeedsY * 2) + bufferSize),
        });
    }

    private void LateUpdate()
    {
        foreach (LayerData layer in layers)
        {
            foreach (Transform child in layer.Container)
            {
                Vector3 childPos = child.position;
                Vector3 camPos = cam.transform.position;

                //Move background tiles to new locations as player travels
                if (camPos.x - childPos.x > layer.TotalWidth / 2)
                {
                    child.Translate(new Vector3(layer.TotalWidth, 0, 0));
                }
                else if (camPos.x - childPos.x < -layer.TotalWidth / 2)
                {
                    child.Translate(new Vector3(-layer.TotalWidth, 0, 0));
                }
                
                if (camPos.y - childPos.y > layer.TotalHeight / 2)
                {
                    child.Translate(new Vector3(0, layer.TotalHeight, 0));
                }
                else if (camPos.y - childPos.y < -layer.TotalHeight / 2)
                {
                    child.Translate(new Vector3(0, -layer.TotalHeight, 0));
                }
            }
        }
    }
}
