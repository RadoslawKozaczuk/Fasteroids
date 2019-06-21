using Unity.Entities;

namespace Assets.Script.Components
{
    public enum CollisionTypeEnum { Player, Laser, Asteroid }

    public struct CollisionTypeData : IComponentData
    {
        public CollisionTypeEnum CollisionObjectType;
    }
}
