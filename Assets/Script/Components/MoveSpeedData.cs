using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Script.Components
{
    public struct MoveSpeedData : IComponentData
    {
        public float DirectionX;
        public float DirectionY;
        public float MoveSpeed;
        public float3 RotationSpeed;
    }
}
