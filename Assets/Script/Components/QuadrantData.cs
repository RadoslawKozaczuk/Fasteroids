using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Components
{
    public struct QuadrantData
    {
        public Entity Entity;
        public float3 EntityPosition;
        public CollisionType CollisionTypeEnum;
    }
}
