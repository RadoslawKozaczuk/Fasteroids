using Unity.Mathematics;

public struct QuadTreeNode
{
    // pointers (not literally but these indexes help us find a node in the super array)
    public readonly int ParentNodeIndex;
    public int TopLeftNodeIndex;
    public int TopRightNodeIndex;
    public int BottomLeftNodeIndex;
    public int BottomRightNodeIndex;

    // tables and their counters (counter says how many elements the particular element hold at the moment)
    public readonly int[] MovableTable; // size predefined
    public int[] PermanentTable;
    public int MovableElementsCounter;
    public int PermanentElementsCounter;

    public readonly float DivisionLineX, DivisionLineY;
    public readonly Quad Boundaries;
    public bool SubDivisionsCreated;

    public QuadTreeNode(in Quad dimensions, int parentNodeIndex = int.MinValue)
    {
        ParentNodeIndex = parentNodeIndex;
        TopLeftNodeIndex = int.MinValue;
        TopRightNodeIndex = int.MinValue;
        BottomLeftNodeIndex = int.MinValue;
        BottomRightNodeIndex = int.MinValue;

        MovableTable = new int[CollisionSystem.MaxEntitiesPerQuad];
        PermanentTable = new int[CollisionSystem.InitialPermaTableLength];
        MovableElementsCounter = 0;
        PermanentElementsCounter = 0;

        DivisionLineX = (dimensions.MinX + dimensions.MaxX) / 2;
        DivisionLineY = (dimensions.MinY + dimensions.MaxY) / 2;
        Boundaries = dimensions;
        SubDivisionsCreated = false;
    }

    /// <summary>
    /// Returns true if the agent with these coordinates is to close to division lines 
    /// to be able to be put deeper in the hierarchy, otherwise false.
    /// </summary>
    public bool TooCloseToDivisionalLines(float agentPosX, float agentPosY)
    {
        float distanceX = DivisionLineX - agentPosX;
        if (distanceX < 0)
            distanceX *= -1;

        if (distanceX < GameEngine.AsteroidRadius)
            return true;

        float distanceY = DivisionLineY - agentPosY;
        if (distanceY < 0)
            distanceY *= -1;

        return distanceY < GameEngine.AsteroidRadius;
    }

    /// <summary>
    /// Checks if the given agent is within the node's boundaries.
    /// This method takes only the agent's position into account not the volume.
    /// </summary>
    public bool WithingBoundaries(in Quad boundaries, float2 position) =>
        position.x >= boundaries.MinX
        && position.x <= boundaries.MaxX
        && position.y >= boundaries.MinY
        && position.y <= boundaries.MaxY;

    #region Assertions
    //public void NoItermidiateMovablesCheck()
    //{
    //    if (MovableElementsCounter > 0 && SubDivisionsCreated)
    //        Debug.LogError($"The quadnode at depth level {_currentDepth} contains movable elements despite having subdivisions.");

    //    if (SubDivisionsCreated)
    //    {
    //        TopLeftNodeIndex.NoItermidiateMovablesCheck();
    //        TopRightNodeIndex.NoItermidiateMovablesCheck();
    //        BottomLeftNodeIndex.NoItermidiateMovablesCheck();
    //        BottomRightNodeIndex.NoItermidiateMovablesCheck();
    //    }
    //}

    //public void NoDeadAgentsCheck()
    //{
    //    for (int i = 0; i < PermanentElementsCounter; i++)
    //        if (GameEngine.Agents[PermanentTable[i]].Flags > 2)
    //            Debug.LogError($"Dead element at depth level {_currentDepth} in permanent table id={PermanentTable[i]}.");

    //    for (int i = 0; i < MovableElementsCounter; i++)
    //        if (GameEngine.Agents[MovableTable[i]].Flags > 2)
    //            Debug.LogError($"Dead element at depth level {_currentDepth} in movable table id={MovableTable[i]}.");

    //    if (SubDivisionsCreated)
    //    {
    //        TopLeftNodeIndex.NoDeadAgentsCheck();
    //        TopRightNodeIndex.NoDeadAgentsCheck();
    //        BottomLeftNodeIndex.NoDeadAgentsCheck();
    //        BottomRightNodeIndex.NoDeadAgentsCheck();
    //    }
    //}

    //public void AgentsNumberCoherencyCheck()
    //{
    //    int agentsNumberTable = 0;

    //    for (int i = 0; i < GameEngine.Agents.Length; i++)
    //        if (GameEngine.Agents[i].Flags < 3)
    //            agentsNumberTable++;

    //    int permanentAgentsNumberInTree = 0;
    //    int movableAgentsNumberInTree = 0;

    //    AgentsNumberCoherencyCheckInternal(ref permanentAgentsNumberInTree, ref movableAgentsNumberInTree);

    //    if (agentsNumberTable != permanentAgentsNumberInTree + movableAgentsNumberInTree)
    //        Debug.LogError($"The number of live agents in the global table ({agentsNumberTable}) is different " +
    //            $"than in the quadtree (perma:{permanentAgentsNumberInTree}, mov:{movableAgentsNumberInTree}).");
    //}

    //void AgentsNumberCoherencyCheckInternal(ref int permaAgentsNumberInTree, ref int movableAgentsNumberInTree)
    //{
    //    permaAgentsNumberInTree += PermanentElementsCounter;
    //    movableAgentsNumberInTree += MovableElementsCounter;

    //    if (SubDivisionsCreated)
    //    {
    //        TopLeftNodeIndex.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
    //        TopRightNodeIndex.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
    //        BottomLeftNodeIndex.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
    //        BottomRightNodeIndex.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
    //    }
    //}

    //public void AgentsOrderCheck()
    //{
    //    for (int i = 0; i < PermanentElementsCounter - 1; i++)
    //        if (GameEngine.Agents[PermanentTable[i]].Position.x > GameEngine.Agents[PermanentTable[i + 1]].Position.x)
    //            Debug.LogError("Agents are not in order! Sort function fail.");

    //    for (int i = 0; i < MovableElementsCounter - 1; i++)
    //        if (GameEngine.Agents[MovableTable[i]].Position.x > GameEngine.Agents[MovableTable[i + 1]].Position.x)
    //            Debug.LogError("Agents are not in order! Sort function fail.");

    //    if (SubDivisionsCreated)
    //    {
    //        TopLeftNodeIndex.AgentsOrderCheck();
    //        TopRightNodeIndex.AgentsOrderCheck();
    //        BottomLeftNodeIndex.AgentsOrderCheck();
    //        BottomRightNodeIndex.AgentsOrderCheck();
    //    }
    //}
    #endregion
}
