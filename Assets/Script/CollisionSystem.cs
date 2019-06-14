using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

public class CollisionSystem
{
    public class AgentComparer : IComparer<int>
    {
        public int Compare(int a, int b) => GameEngine.Agents[a].Position.x >= GameEngine.Agents[b].Position.x ? 1 : -1;
    }

    public static QuadTreeNode[] NodeArray = new QuadTreeNode[4500];
    public static int MaxEntitiesPerQuad;
    public static int InitialPermaTableLength;
    readonly AgentComparer _agentComparer = new AgentComparer();
    int _nodeArrayCounter = 0;

    public CollisionSystem(in Quad boundaries, int maxEntitiesPerQuad, int initialPermaTableLength)
    {
        InitialPermaTableLength = initialPermaTableLength;
        MaxEntitiesPerQuad = maxEntitiesPerQuad;

        // create root node
        NodeArray[_nodeArrayCounter++] = new QuadTreeNode(in boundaries, 0);
    }

    /// <summary>
    /// Return index in the super table where the element is stored.
    /// </summary>
    public int CreateNode(in Quad boundaries, int parentNodeIndex)
    {
        // for now without safety checks
        NodeArray[_nodeArrayCounter] = new QuadTreeNode(in boundaries, parentNodeIndex);
        _nodeArrayCounter++;
        return _nodeArrayCounter - 1;
    }

    public void GenerateQuadTree(Agent[] asteroids)
    {
        CreateSubdivisions(0);

        for (int i = 0; i < asteroids.Length; i++)
        {
            // dead elements are not added to the tree
            if (GameEngine.Agents[i].Flags < 3)
                Add(0, i);
        }
    }

    public void Add(int targetNodeIndex, int agentId)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        // first check if the element can not be put in subdivision due to its proximity to the division lines
        if (node.TooCloseToDivisionalLines(GameEngine.Agents[agentId].Position.x, GameEngine.Agents[agentId].Position.y))
        {
            // add to perma list
            node.PermanentTable[node.PermanentElementsCounter++] = agentId;

            // resize the table
            if (node.PermanentElementsCounter == node.PermanentTable.Length)
            {
                // use array copy as it is faster and safer
                int[] tempTable = new int[node.PermanentElementsCounter];
                Buffer.BlockCopy(node.PermanentTable, 0, tempTable, 0, node.PermanentElementsCounter * 4);

                node.PermanentTable = new int[node.PermanentElementsCounter * 2];
                Buffer.BlockCopy(tempTable, 0, node.PermanentTable, 0, node.PermanentElementsCounter * 4);
            }

            return;
        }

        if (node.SubDivisionsCreated)
        {
            AddToSubQuad(targetNodeIndex, agentId); // recursive call
            return; // agent added to one of the subquads
        }

        if (node.MovableElementsCounter < MaxEntitiesPerQuad)
        {
            node.MovableTable[node.MovableElementsCounter++] = agentId;

            return; // agent added to this quad
        }
        else
        {
            // additional subdivision needed
            CreateSubdivisions(targetNodeIndex);
            AddToSubQuad(targetNodeIndex, agentId); // add this to one of the subquads

            while (node.MovableElementsCounter > 0)
            {
                // go from the end to the start
                AddToSubQuad(targetNodeIndex, node.MovableTable[--node.MovableElementsCounter]);
            }
        }
    }

    public void CreateSubdivisions(int callerNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[callerNodeIndex];
        node.SubDivisionsCreated = true;

        float minX = node.Boundaries.MinX, maxX = node.Boundaries.MaxX, minY = node.Boundaries.MinY, maxY = node.Boundaries.MaxY;
        node.TopLeftNodeIndex = CreateNode(new Quad(minX, maxX - (maxX - minX) / 2, maxY - (maxY - minY) / 2, maxY), callerNodeIndex);
        node.TopRightNodeIndex = CreateNode(new Quad(maxX - (maxX - minX) / 2, maxX, maxY - (maxY - minY) / 2, maxY), callerNodeIndex);
        node.BottomLeftNodeIndex = CreateNode(new Quad(minX, maxX - (maxX - minX) / 2, minY, maxY - (maxY - minY) / 2), callerNodeIndex);
        node.BottomRightNodeIndex = CreateNode(new Quad(maxX - (maxX - minX) / 2, maxX, minY, maxY - (maxY - minY) / 2), callerNodeIndex);
    }

    /// <summary>
    /// Used to move an agent upwards in the hierarchy.
    /// This method calls itself recursively until it founds a suitable node to put the agent into.
    /// TargetNodeIndex means index of the parent node.
    /// </summary>
    void AddFromBottom(int targetNodeIndex, int agentId)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        if (node.WithingBoundaries(in node.Boundaries, GameEngine.Agents[agentId].Position))
            Add(targetNodeIndex, agentId);
        else
            AddFromBottom(node.ParentNodeIndex, agentId);
    }

    void AddToSubQuad(int callerNodeId, int id)
    {
        ref QuadTreeNode node = ref NodeArray[callerNodeId];

        float2 pos = GameEngine.Agents[id].Position;

        if (pos.x >= node.DivisionLineX)
        {
            // right side
            if (pos.y >= node.DivisionLineY)
                Add(node.TopRightNodeIndex, id);
            else
                Add(node.BottomRightNodeIndex, id);
        }
        else
        {
            // left side
            if (pos.y >= node.DivisionLineY)
                Add(node.TopLeftNodeIndex, id);
            else
                Add(node.BottomLeftNodeIndex, id);
        }
    }

    /// <summary>
    /// Sort all elements in all tables by their position X in this node and 
    /// in all subsequent nodes.
    /// </summary>
    public void SortElements(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        if (node.PermanentElementsCounter > 1)
            Array.Sort(node.PermanentTable, 0, node.PermanentElementsCounter, _agentComparer);

        if (node.MovableElementsCounter > 1)
            Array.Sort(node.MovableTable, 0, node.MovableElementsCounter, _agentComparer);

        if (node.SubDivisionsCreated)
        {
            SortElements(node.TopLeftNodeIndex);
            SortElements(node.TopRightNodeIndex);
            SortElements(node.BottomLeftNodeIndex);
            SortElements(node.BottomRightNodeIndex);
        }
    }

    /// <summary>
    /// Sort all elements in all tables by their position X.
    /// </summary>
    public void SortElementsOnlyThisNode(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        if (node.PermanentElementsCounter > 1)
            Array.Sort(node.PermanentTable, 0, node.PermanentElementsCounter, _agentComparer);

        if (node.MovableElementsCounter > 1)
            Array.Sort(node.MovableTable, 0, node.MovableElementsCounter, _agentComparer);
    }

    /// <summary>
    /// Removed dead elements (by replacing it with the last element and decrementing the counter)
    /// in this node and all subsequent nodes.
    /// </summary>
    public void RemoveDeadElements(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        int i = 0;
        while (i < node.PermanentElementsCounter)
        {
            if (GameEngine.Agents[node.PermanentTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --node.PermanentElementsCounter)
                    node.PermanentTable[i] = node.PermanentTable[node.PermanentElementsCounter];
                continue;
            }
            i++;
        }

        i = 0;
        while (i < node.MovableElementsCounter)
        {
            if (GameEngine.Agents[node.MovableTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --node.MovableElementsCounter)
                    node.MovableTable[i] = node.MovableTable[node.MovableElementsCounter];
                continue;
            }
            i++;
        }

        if (node.SubDivisionsCreated)
        {
            RemoveDeadElements(node.TopLeftNodeIndex);
            RemoveDeadElements(node.TopRightNodeIndex);
            RemoveDeadElements(node.BottomLeftNodeIndex);
            RemoveDeadElements(node.BottomRightNodeIndex);
        }
    }

    /// <summary>
    /// Removed dead elements (by replacing it with the last element and decrementing the counter)
    /// in this node and all subsequent nodes.
    /// </summary>
    public void RemoveDeadElementsOnlyThisNode(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        int i = 0;
        while (i < node.PermanentElementsCounter)
        {
            if (GameEngine.Agents[node.PermanentTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --node.PermanentElementsCounter)
                    node.PermanentTable[i] = node.PermanentTable[node.PermanentElementsCounter];
                continue;
            }
            i++;
        }

        i = 0;
        while (i < node.MovableElementsCounter)
        {
            if (GameEngine.Agents[node.MovableTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --node.MovableElementsCounter)
                    node.MovableTable[i] = node.MovableTable[node.MovableElementsCounter];
                continue;
            }
            i++;
        }
    }

    /// <summary>
    /// Updates all agents in terms of theirs position withing the quadtree structure.
    /// Additionally removes those elements that are marked as dead.
    /// Dead elements will be reinserted when necessary by other functions.
    /// </summary>
    public void UpdateNode(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        int i = 0;
        while (i < node.PermanentElementsCounter)
        {
            int permaObjectId = node.PermanentTable[i];
            ref Agent a = ref GameEngine.Agents[permaObjectId];

            if (a.Flags > 2) // dead
            {
                i++;
                continue;
            }

            // no longer in the quad
            if (!node.WithingBoundaries(in node.Boundaries, a.Position))
            {
                // remove from perm by inserting the last one on current spot
                if (i < --node.PermanentElementsCounter)
                    node.PermanentTable[i] = node.PermanentTable[node.PermanentElementsCounter];

                AddFromBottom(node.ParentNodeIndex, permaObjectId);
                continue;
            }

            // got out of the division lines
            if (!node.TooCloseToDivisionalLines(a.Position.x, a.Position.y))
            {
                if (node.SubDivisionsCreated)
                    AddToSubQuad(targetNodeIndex, permaObjectId); // add this to one of the subquads

                // still enough space
                else if (node.MovableElementsCounter < MaxEntitiesPerQuad)
                    node.MovableTable[node.MovableElementsCounter++] = permaObjectId;
                else
                {
                    // additional subdivision needed
                    CreateSubdivisions(targetNodeIndex);
                    AddToSubQuad(targetNodeIndex, permaObjectId); // add this to one of the subquads

                    while (node.MovableElementsCounter > 0)
                        AddToSubQuad(targetNodeIndex, node.MovableTable[--node.MovableElementsCounter]); // go from the end to the start
                }

                // if this is the last one simply decrease the counter
                // otherwise put the last one's value in the i's spot and decrease the counter by 1
                if (i < --node.PermanentElementsCounter)
                    node.PermanentTable[i] = node.PermanentTable[node.PermanentElementsCounter];

                continue;
            }

            i++;
        }

        i = 0;
        while (i < node.MovableElementsCounter)
        {
            int movaObjectId = node.MovableTable[i];
            ref Agent a = ref GameEngine.Agents[movaObjectId];

            if (a.Flags > 2) // dead
            {
                i++;
                continue;
            }

            // are you still in this quad?
            if (!node.WithingBoundaries(in node.Boundaries, a.Position))
            {
                // if this is the last one simply decrease the counter
                // otherwise put the last one's value in the i's spot and decrease the counter by 1
                if (i < --node.MovableElementsCounter)
                    node.MovableTable[i] = node.MovableTable[node.MovableElementsCounter];

                AddFromBottom(node.ParentNodeIndex, movaObjectId);
                continue;
            }

            // got to close to the division lines
            if (node.TooCloseToDivisionalLines(a.Position.x, a.Position.y))
            {
                // add to perma list
                node.PermanentTable[node.PermanentElementsCounter++] = movaObjectId;

                // resize the table if necessary
                if (node.PermanentElementsCounter == node.PermanentTable.Length)
                {
                    // extend the table two times
                    int[] tempTable = new int[node.PermanentElementsCounter];
                    Buffer.BlockCopy(node.PermanentTable, 0, tempTable, 0, node.PermanentElementsCounter * 4);

                    node.PermanentTable = new int[node.PermanentElementsCounter * 2];
                    Buffer.BlockCopy(tempTable, 0, node.PermanentTable, 0, node.PermanentElementsCounter * 4);
                }

                // if this is the last one simply decrease the counter
                // otherwise put the last one's value in the i's spot and decrease the counter by 1
                if (i < --node.MovableElementsCounter)
                    node.MovableTable[i] = node.MovableTable[node.MovableElementsCounter];

                continue;
            }

            i++;
        }

        if (node.SubDivisionsCreated)
        {
            UpdateNode(node.TopLeftNodeIndex);
            UpdateNode(node.TopRightNodeIndex);
            UpdateNode(node.BottomLeftNodeIndex);
            UpdateNode(node.BottomRightNodeIndex);
        }
    }

    public void CheckCollisions(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        // elements are always sorted by positionX
        if (node.PermanentElementsCounter > 0)
            CheckCollisionsPermanent(targetNodeIndex);

        if (node.SubDivisionsCreated)
        {
            CheckCollisions(node.TopLeftNodeIndex);
            CheckCollisions(node.TopRightNodeIndex);
            CheckCollisions(node.BottomLeftNodeIndex);
            CheckCollisions(node.BottomRightNodeIndex);
        }
        else
        {
            // movable can only be at the bottom of the tree so recursion is not needed
            CheckCollisionsMovable(targetNodeIndex);
        }
    }

    public void CheckCollisionsOnlyThisNode(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        if (node.PermanentElementsCounter > 0)
            CheckCollisionsPermanent(targetNodeIndex);

        if (!node.SubDivisionsCreated)
            CheckCollisionsMovable(targetNodeIndex);
    }

    /// <summary>
    /// This method is faster as it takes advantage of the fact that the passed element's x position 
    /// is always greater than in all elements in this node.
    /// </summary>
    public void CheckCollisionsLeft(int targetNodeIndex, ref Agent a)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        // check a with all perms and movables in this node
        // if it contains subdivisions go and call this recursively
        if (node.PermanentElementsCounter > 0)
        {
            for (int i = node.PermanentElementsCounter - 1; i >= 0; i--)
            {
                ref Agent b = ref GameEngine.Agents[node.PermanentTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = a.Position.x - b.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= GameEngine.AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }

        // check if a is still alive
        if (a.Flags < 3 && node.SubDivisionsCreated)
        {
            CheckCollisionsLeft(node.TopLeftNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisionsLeft(node.TopRightNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisionsLeft(node.BottomLeftNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisionsLeft(node.BottomRightNodeIndex, ref a);
        }
        else
        {
            for (int i = node.MovableElementsCounter - 1; i > 0; i--)
            {
                ref Agent b = ref GameEngine.Agents[node.MovableTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = a.Position.x - b.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= GameEngine.AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }
    }

    /// <summary>
    /// This method is faster as it takes advantage of the fact that the passed element's x position 
    /// is always smaller than in all elements in this node.
    /// </summary>
    public void CheckCollisionsRight(int targetNodeIndex, ref Agent a)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        // check a with all perms and movables in this node
        // if it contains subdivisions go and call this recursively
        if (node.PermanentElementsCounter > 0)
        {
            for (int i = 0; i < node.PermanentElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[node.PermanentTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= GameEngine.AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }

        // check if a is still alive
        if (a.Flags < 3 && node.SubDivisionsCreated)
        {
            CheckCollisionsRight(node.TopLeftNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisionsRight(node.TopRightNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisionsRight(node.BottomLeftNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisionsRight(node.BottomRightNodeIndex, ref a);
        }
        else
        {
            for (int i = 0; i < node.MovableElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[node.MovableTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= GameEngine.AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }
    }

    /// <summary>
    /// For cost efficiency this method does not check if the agents are alive.
    /// It is always the responsibility of the caller function to do that.
    /// </summary>
    bool CheckCollisionBetweenAgents(ref Agent a, ref Agent b)
    {
        float difX = b.Position.x - a.Position.x;
        if (difX < 0)
            difX *= -1;

        if (difX >= GameEngine.AsteroidRadius2)
            return false; // no collision possible

        float difY = b.Position.y - a.Position.y;
        if (difY < 0)
            difY *= -1;

        if (difY >= GameEngine.AsteroidRadius2)
            return false; // no collision possible

        float distance = FastSqrt(difX * difX + difY * difY);
        if (distance >= GameEngine.AsteroidRadius2)
            return false; // no collision possible

        // collision detected
        if (a.Flags == 2) // a is an asteroid
        {
            a.TimeLeftToRespawn = 1f;
            a.Flags = 5; // destroyed asteroid

            if (b.Flags == 2) // b is an asteroid
            {
                b.TimeLeftToRespawn = 1f;
                b.Flags = 5;
            }
            else if (b.Flags == 1) // b is a laser beam
            {
                b.Flags = 4; // destroyed laser beam
                GameEngine.NumberOfAsteroidsDestroyedThisFrame++;
            }
            else // b is a spaceship
            {
                b.Flags = 3;
                GameEngine.DidPlayerDieThisFrame = true;
            }

            return true;
        }
        else if (a.Flags == 1) // a is a laser beam
        {
            // laser beam cannot collide with anything else than an asteroid
            a.Flags = 4; // destroyed laser beam
            b.TimeLeftToRespawn = 1f;
            b.Flags = 5; // destroyed asteroid
            GameEngine.NumberOfAsteroidsDestroyedThisFrame++;

            return true;
        }
        else // a is a player
        {
            // player cannot collide with anything else than an asteroid
            a.Flags = 3; // destroyed spaceship
            b.Flags = 5; // destroyed asteroid
            GameEngine.DidPlayerDieThisFrame = true;

            return true;
        }
    }


    public void CheckCollisionsPermanent(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        // perform checks for this agent with all the other in the permanent table
        for (int i = 0; i < node.PermanentElementsCounter; i++)
        {
            // check this perm with all other perms
            ref Agent a = ref GameEngine.Agents[node.PermanentTable[i]];

            if (a.Flags > 2)
                continue;

            int j = i;
            while (++j < node.PermanentElementsCounter)
            {
                ref Agent b = ref GameEngine.Agents[node.PermanentTable[j]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the while loop
                float difX = b.Position.x - a.Position.x;

                // difX can never be negative in this context because we compare elements from the same table
                if (difX >= GameEngine.AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    break;
            }

            if (a.Flags > 2)
                continue;

            // then check this perm with all movables
            for (int k = 0; k < node.MovableElementsCounter; k++)
            {
                ref Agent b = ref GameEngine.Agents[node.MovableTable[k]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;
                if (difX < 0)
                    difX *= -1;

                if (difX >= GameEngine.AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    break;
            }

            // the agent is still alive and we can continue searching by going down the tree
            if (a.Flags < 3 && node.SubDivisionsCreated)
            {
                // check if this one can collide with any one from any subdivisions

                float distanceX = node.DivisionLineX - a.Position.x;
                if (distanceX < 0)
                    distanceX *= -1;

                // a is at the divisional line - optimizations can be performed
                if (distanceX < GameEngine.AsteroidRadius)
                {
                    CheckCollisionsLeft(node.TopLeftNodeIndex, ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    CheckCollisionsRight(node.TopRightNodeIndex, ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    CheckCollisionsLeft(node.BottomLeftNodeIndex, ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    CheckCollisionsRight(node.BottomRightNodeIndex, ref a);
                }
                else
                {
                    CheckCollisions(node.TopLeftNodeIndex, ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    CheckCollisions(node.TopRightNodeIndex, ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    CheckCollisions(node.BottomLeftNodeIndex, ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    CheckCollisions(node.BottomRightNodeIndex, ref a);
                }
            }
        }
    }

    public void CheckCollisionsMovable(int targetNodeIndex)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        for (int i = 0; i < node.MovableElementsCounter - 1; i++)
        {
            ref Agent a = ref GameEngine.Agents[node.MovableTable[i]];
            if (a.Flags > 2)
                continue;

            int j = i + 1;
            do
            {
                ref Agent b = ref GameEngine.Agents[node.MovableTable[j]];
                if (b.Flags > 2)
                {
                    if (++j < node.MovableElementsCounter)
                        continue;
                    else
                        break;
                }

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;

                // difX can never be negative in this context because we compare elements from the same table
                if (difX >= GameEngine.AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    break;

                j++;
            }
            while (j < node.MovableElementsCounter);
        }
    }

    void CheckCollisions(int targetNodeIndex, ref Agent a)
    {
        ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

        // check a with all perms and movables in this node
        // if it contains subdivisions go and call this recursively
        if (node.PermanentElementsCounter > 0)
        {
            for (int i = 0; i < node.PermanentElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[node.PermanentTable[i]];
                if (b.Flags > 2)
                    continue;

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }

        // check if a is still alive
        if (a.Flags < 3 && node.SubDivisionsCreated)
        {
            CheckCollisions(node.TopLeftNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisions(node.TopRightNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisions(node.BottomLeftNodeIndex, ref a);
            if (a.Flags > 2) // died in the check above
                return;

            CheckCollisions(node.BottomRightNodeIndex, ref a);
        }
        else
        {
            for (int i = 0; i < node.MovableElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[node.MovableTable[i]];
                if (b.Flags > 2)
                    continue;

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }
    }

    #region Fast Sqrt
    [StructLayout(LayoutKind.Explicit)]
    struct FloatIntUnion
    {
        [FieldOffset(0)]
        public float Flt;

        [FieldOffset(0)]
        public int Tmp;
    }

    // not written by me, I found it on the Internet
    // it is around 10 - 15% faster than the Mathf.Sqrt from Unity.Mathematics 
    // (which probably uses the inverse square root method from Quake 3 based on its cost).
    // but that comes for a cost of less accurate approximation (from 0.5% to 5% less accurate)
    float FastSqrt(float number)
    {
        if (number == 0)
            return 0;

        FloatIntUnion u;
        u.Tmp = 0;
        u.Flt = number;
        u.Tmp -= 1 << 23; /* Subtract 2^m. */
        u.Tmp >>= 1; /* Divide by 2. */
        u.Tmp += 1 << 29; /* Add ((b + 1) / 2) * 2^m. */
        return u.Flt;
    }
    #endregion
}
