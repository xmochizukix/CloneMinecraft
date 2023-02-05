using UnityEngine;

[CreateAssetMenu(menuName = "Blocks/Sides block")]
public class BlockInfoSides : BlockInfo
{
    public Vector2 PixelsOffsetUp;
    public Vector2 PixelsOffsetDown;

    public override UnityEngine.Vector2 GetPixelOffset(Vector3Int normal)
    {
        if(normal == Vector3Int.up) return PixelsOffsetUp;
        if(normal == Vector3Int.down) return PixelsOffsetDown;
        
        return base.GetPixelOffset(normal);
    }
}
