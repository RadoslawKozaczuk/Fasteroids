public class CollisionSystem
{
    public readonly QuadTreeNode RootNode;
    public readonly int MaxEntitiesPerQuad;
    public readonly int InitialPermaTableLength;

    public CollisionSystem(in Quad boundaries, int maxEntitiesPerQuad, int initialPermaTableLength)
    {
        InitialPermaTableLength = initialPermaTableLength;
        MaxEntitiesPerQuad = maxEntitiesPerQuad;
        RootNode = new QuadTreeNode(this, null, in boundaries, 0);
    }

    public void GenerateQuadTree(Agent[] asteroids)
    {
        RootNode.CreateSubdivisions();

        for (int i = 0; i < asteroids.Length; i++)
        {
            // dead elements are not added to the tree
            if (GameEngine.Agents[i].Flags < 3)
                RootNode.Add(i);
        }
    }

    public void UpdateTreeStructure() => RootNode.UpdateNode();

    public void SortElements() => RootNode.SortElements();

    public void SortElementsOnlyThisNode() => RootNode.SortElementsOnlyThisNode();

    public void RemoveDeadElements() => RootNode.RemoveDeadElements();

    public void CheckCollisions() => RootNode.CheckCollisions();

    public void CheckCollisionsOnlyThisNode() => RootNode.CheckCollisionsOnlyThisNode();
}
