using Unity.Entities;

namespace Assets.Scripts.Components
{
    public enum CollisionType { Player, Laser, Asteroid }

    public struct CollisionTypeData : IComponentData
    {
        public CollisionType CollisionObjectType;
    }
}
