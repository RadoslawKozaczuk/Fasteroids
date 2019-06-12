using Unity.Mathematics;

// beautiful memory layout exactly 16 bytes
public struct Agent
{
    // float2 to gain some speed by using matrix operations
    public float2 Position;
    public float TimeLeftToRespawn;

    // we use lookup table for these to achieve good memory layout
    public byte Speed; // lookup table speed
    public byte DirectionX; // lookup table dirX
    public byte DirectionY; // lookup table dirY

    // byte is blittable, smaller and can store more than one state - simply better
    // 0 - normal happy spaceship
    // 1 - normal happy laser beam
    // 2 - normal happy asteroid
    // 3 - destroyed spaceship
    // 4 - destroyed laser beam
    // 5 - destroyed asteroid
    public byte Flags;
}