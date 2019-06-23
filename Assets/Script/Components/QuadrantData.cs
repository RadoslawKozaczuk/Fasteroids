using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Script.Components
{
    public struct QuadrantData
    {
        public Entity Entity;
        public float3 EntityPosition;
        public CollisionTypeEnum CollisionTypeEnum;
    }
}
