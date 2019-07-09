using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class UpdateGroup1 : ComponentSystemGroup { } // executes first
    public class UpdateGroup2 : ComponentSystemGroup { }
    public class UpdateGroup3 : ComponentSystemGroup { }
    public class UpdateGroup4 : ComponentSystemGroup { } // executes last
}
