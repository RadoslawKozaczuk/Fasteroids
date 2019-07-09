using Unity.Entities;

namespace Assets.Script.Components
{
    public enum CollisionType { Player, Laser, Asteroid }

    public struct CollisionTypeData : IComponentData
    {
        public CollisionType CollisionObjectType;
    }
}
