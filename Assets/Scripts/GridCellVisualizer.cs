using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellVisualizer : MonoBehaviour
{
    //declare serialized object reference variables for interior grid sprite
    [SerializeField]
    private SpriteRenderer _interiorSprite;

    public GridCellData gridCellData;

    //create methods for setting the color of the interior sprite
    public void SetInteriorColor(Color color) => _interiorSprite.color = color;

}
