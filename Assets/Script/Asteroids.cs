using Unity.Mathematics;

// beautiful memory layout exactly 16 bytes
public struct Asteroid
{
    // float2 to gain some speed by using matrix operations
    public float2 Position;
    public float TimeLeftToRespawn;

    // we use lookup table for these to achieve good memory layout
    public byte Speed; // lookup table speed
    public byte DirectionX; // lookup table dirX
    public byte DirectionY; // lookup table dirY

    // byte is blittable, smaller and can store more than one state - simply better
    public byte Flags; // 0 - nothing, 1 - destroyed this frame, 2 - destroyed, waiting for respawn
}
