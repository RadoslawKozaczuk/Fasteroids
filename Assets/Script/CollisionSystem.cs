using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Mathematics;

public class CollisionSystem
{
    //public class AgentComparer : IComparer<int>
    //{
    //    public int Compare(int a, int b) => GameEngine.Agents[a].Position.x >= GameEngine.Agents[b].Position.x ? 1 : -1;
    //}

    //public static QuadTreeNode[] NodeArray = new QuadTreeNode[6000];
    //public static int MaxEntitiesPerQuad;
    //public static int InitialPermaTableLength;
    //readonly AgentComparer _agentComparer = new AgentComparer();
    //int _nodeArrayCounter = 0;
    //object _createNodeLock = new object();

    //public CollisionSystem(in Quad boundaries, int maxEntitiesPerQuad, int initialPermaTableLength, int initialSubDivisionLevel = 0)
    //{
    //    InitialPermaTableLength = initialPermaTableLength;
    //    MaxEntitiesPerQuad = maxEntitiesPerQuad;

    //    // create root node
    //    NodeArray[_nodeArrayCounter++] = new QuadTreeNode(in boundaries, 0);

    //    CreateInitialSubdivisions(initialSubDivisionLevel);
    //}

    //void CreateInitialSubdivisions(int level)
    //{
    //    int multiplier = 4;
    //    int nodesToCreate = 1;
    //    for (int i = 1; i < level; i++)
    //    {
    //        nodesToCreate += multiplier;
    //        multiplier *= 4;
    //    }

    //    for (int i = 0; i < nodesToCreate; i++)
    //        CreateSubdivisions(i);
    //}

    ///// <summary>
    ///// Return index in the super table where the element is stored.
    ///// </summary>
    //public int CreateNode(in Quad boundaries, int parentNodeIndex)
    //{
    //    // for now without safety checks
    //    lock(_createNodeLock)
    //    {
    //        NodeArray[_nodeArrayCounter] = new QuadTreeNode(in boundaries, parentNodeIndex);
    //        _nodeArrayCounter++;
    //        return _nodeArrayCounter - 1;
    //    }
    //}

    //public void AddElementsToQuadTree(Agent[] asteroids)
    //{
    //    var tlList = new List<int>(asteroids.Length / 4);
    //    var trList = new List<int>(asteroids.Length / 4);
    //    var blList = new List<int>(asteroids.Length / 4);
    //    var brList = new List<int>(asteroids.Length / 4);

    //    ref QuadTreeNode rootNode = ref NodeArray[0];
    //    int tlIndex = rootNode.TopLeftNodeIndex;
    //    int trIndex = rootNode.TopRightNodeIndex;
    //    int blIndex = rootNode.BottomLeftNodeIndex;
    //    int brIndex = rootNode.BottomRightNodeIndex;

    //    for (int i = 0; i < asteroids.Length; i++)
    //    {
    //        // dead elements are not added to the tree
    //        if (GameEngine.Agents[i].Flags < 3)
    //        {
    //            float2 pos = GameEngine.Agents[i].Position;

    //            // if can be added to the root perma table do it
    //            if (rootNode.TooCloseToDivisionalLines(pos.x, pos.y))
    //            {
    //                // add to root's perma table
    //                rootNode.AddToPermaTable(i);
    //            }
    //            // otherwise add it to one of the four insertion lists
    //            else
    //            {
    //                if (pos.x >= rootNode.DivisionLineX)
    //                {
    //                    // right side
    //                    if (pos.y >= rootNode.DivisionLineY)
    //                        trList.Add(i);
    //                    else
    //                        brList.Add(i);
    //                }
    //                else
    //                {
    //                    // left side
    //                    if (pos.y >= rootNode.DivisionLineY)
    //                        tlList.Add(i);
    //                    else
    //                        blList.Add(i);
    //                }
    //            }
    //        }
    //    }

    //    // then run each list on a different core
    //    Task t1 = Task.Factory.StartNew(() =>
    //    {
    //        for (int i = 0; i < tlList.Count; i++)
    //            Add(tlIndex, tlList[i]);
    //    });

    //    Task t2 = Task.Factory.StartNew(() =>
    //    {
    //        for (int i = 0; i < trList.Count; i++)
    //            Add(trIndex, trList[i]);
    //    });

    //    Task t3 = Task.Factory.StartNew(() =>
    //    {
    //        for (int i = 0; i < blList.Count; i++)
    //            Add(blIndex, blList[i]);
    //    });

    //    Task t4 = Task.Factory.StartNew(() =>
    //    {
    //        for (int i = 0; i < brList.Count; i++)
    //            Add(brIndex, brList[i]);
    //    });

    //    Task.WaitAll(t1, t2, t3, t4);
    //}

    //public void Add(int targetNodeIndex, int agentId)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    // first check if the element can not be put in subdivision due to its proximity to the division lines
    //    if (node.TooCloseToDivisionalLines(GameEngine.Agents[agentId].Position.x, GameEngine.Agents[agentId].Position.y))
    //    {
    //        node.AddToPermaTable(agentId);
    //        return;
    //    }

    //    if (node.SubDivisionsCreated)
    //    {
    //        AddToSubQuad(targetNodeIndex, agentId); // recursive call
    //        return; // agent added to one of the subquads
    //    }

    //    if (node.MovableElementsCounter < MaxEntitiesPerQuad)
    //    {
    //        node.MovableTable[node.MovableElementsCounter++] = agentId;

    //        return; // agent added to this quad
    //    }
    //    else
    //    {
    //        // additional subdivision needed
    //        CreateSubdivisions(targetNodeIndex);
    //        AddToSubQuad(targetNodeIndex, agentId); // add this to one of the subquads

    //        while (node.MovableElementsCounter > 0)
    //        {
    //            // go from the end to the start
    //            AddToSubQuad(targetNodeIndex, node.MovableTable[--node.MovableElementsCounter]);
    //        }
    //    }
    //}

    //public void CreateSubdivisions(int callerNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[callerNodeIndex];
    //    node.SubDivisionsCreated = true;

    //    float minX = node.Boundaries.MinX, maxX = node.Boundaries.MaxX, minY = node.Boundaries.MinY, maxY = node.Boundaries.MaxY;
    //    node.TopLeftNodeIndex = CreateNode(new Quad(minX, maxX - (maxX - minX) / 2, maxY - (maxY - minY) / 2, maxY), callerNodeIndex);
    //    node.TopRightNodeIndex = CreateNode(new Quad(maxX - (maxX - minX) / 2, maxX, maxY - (maxY - minY) / 2, maxY), callerNodeIndex);
    //    node.BottomLeftNodeIndex = CreateNode(new Quad(minX, maxX - (maxX - minX) / 2, minY, maxY - (maxY - minY) / 2), callerNodeIndex);
    //    node.BottomRightNodeIndex = CreateNode(new Quad(maxX - (maxX - minX) / 2, maxX, minY, maxY - (maxY - minY) / 2), callerNodeIndex);
    //}

    ///// <summary>
    ///// Used to move an agent upwards in the hierarchy.
    ///// This method calls itself recursively until it founds a suitable node to put the agent into.
    ///// TargetNodeIndex means index of the parent node.
    ///// </summary>
    //void AddFromBottom(int targetNodeIndex, int agentId)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    if (node.WithingBoundaries(in node.Boundaries, GameEngine.Agents[agentId].Position))
    //        Add(targetNodeIndex, agentId);
    //    else
    //        AddFromBottom(node.ParentNodeIndex, agentId);
    //}

    //void AddToSubQuad(int callerNodeId, int id)
    //{
    //    ref QuadTreeNode node = ref NodeArray[callerNodeId];

    //    float2 pos = GameEngine.Agents[id].Position;

    //    if (pos.x >= node.DivisionLineX)
    //    {
    //        // right side
    //        if (pos.y >= node.DivisionLineY)
    //            Add(node.TopRightNodeIndex, id);
    //        else
    //            Add(node.BottomRightNodeIndex, id);
    //    }
    //    else
    //    {
    //        // left side
    //        if (pos.y >= node.DivisionLineY)
    //            Add(node.TopLeftNodeIndex, id);
    //        else
    //            Add(node.BottomLeftNodeIndex, id);
    //    }
    //}

    ///// <summary>
    ///// Sort all elements in all tables by their position X in this node and 
    ///// in all subsequent nodes.
    ///// </summary>
    //public void SortElements(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    if (node.PermanentElementsCounter > 1)
    //        Array.Sort(node.PermanentTable, 0, node.PermanentElementsCounter, _agentComparer);

    //    if (node.MovableElementsCounter > 1)
    //        Array.Sort(node.MovableTable, 0, node.MovableElementsCounter, _agentComparer);

    //    if (node.SubDivisionsCreated)
    //    {
    //        SortElements(node.TopLeftNodeIndex);
    //        SortElements(node.TopRightNodeIndex);
    //        SortElements(node.BottomLeftNodeIndex);
    //        SortElements(node.BottomRightNodeIndex);
    //    }
    //}

    ///// <summary>
    ///// Sort all elements in all tables by their position X.
    ///// </summary>
    //public void SortElementsOnlyThisNode(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    if (node.PermanentElementsCounter > 1)
    //        Array.Sort(node.PermanentTable, 0, node.PermanentElementsCounter, _agentComparer);

    //    if (node.MovableElementsCounter > 1)
    //        Array.Sort(node.MovableTable, 0, node.MovableElementsCounter, _agentComparer);
    //}

    ///// <summary>
    ///// Removed dead elements (by replacing it with the last element and decrementing the counter)
    ///// in this node and all subsequent nodes.
    ///// </summary>
    //public void RemoveDeadElements(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    int i = 0;
    //    while (i < node.PermanentElementsCounter)
    //    {
    //        if (GameEngine.Agents[node.PermanentTable[i]].Flags > 2) // dead
    //        {
    //            node.RemoveFromPermaTable(i);
    //            continue;
    //        }
    //        i++;
    //    }

    //    i = 0;
    //    while (i < node.MovableElementsCounter)
    //    {
    //        if (GameEngine.Agents[node.MovableTable[i]].Flags > 2) // dead
    //        {
    //            node.RemoveFromMovableTable(i);
    //            continue;
    //        }
    //        i++;
    //    }

    //    if (node.SubDivisionsCreated)
    //    {
    //        RemoveDeadElements(node.TopLeftNodeIndex);
    //        RemoveDeadElements(node.TopRightNodeIndex);
    //        RemoveDeadElements(node.BottomLeftNodeIndex);
    //        RemoveDeadElements(node.BottomRightNodeIndex);
    //    }
    //}

    ///// <summary>
    ///// Removed dead elements (by replacing it with the last element and decrementing the counter)
    ///// in this node and all subsequent nodes.
    ///// </summary>
    //public void RemoveDeadElementsOnlyThisNode(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    int i = 0;
    //    while (i < node.PermanentElementsCounter)
    //    {
    //        if (GameEngine.Agents[node.PermanentTable[i]].Flags > 2) // dead
    //        {
    //            node.RemoveFromPermaTable(i);
    //            continue;
    //        }
    //        i++;
    //    }

    //    i = 0;
    //    while (i < node.MovableElementsCounter)
    //    {
    //        if (GameEngine.Agents[node.MovableTable[i]].Flags > 2) // dead
    //        {
    //            node.RemoveFromMovableTable(i);
    //            continue;
    //        }
    //        i++;
    //    }
    //}

    ///// <summary>
    ///// Updates all agents in terms of theirs position withing the quadtree structure.
    ///// Additionally removes those elements that are marked as dead.
    ///// Dead elements will be reinserted when necessary by other functions.
    ///// </summary>
    //public void UpdateNode(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    int i = 0;
    //    while (i < node.PermanentElementsCounter)
    //    {
    //        int permaObjectId = node.PermanentTable[i];
    //        ref Agent a = ref GameEngine.Agents[permaObjectId];

    //        if (a.Flags > 2) // dead
    //        {
    //            i++;
    //            continue;
    //        }

    //        // no longer in the quad
    //        if (!node.WithingBoundaries(in node.Boundaries, a.Position))
    //        {
    //            node.RemoveFromPermaTable(i);
    //            AddFromBottom(node.ParentNodeIndex, permaObjectId);
    //            continue;
    //        }

    //        // got out of the division lines
    //        if (!node.TooCloseToDivisionalLines(a.Position.x, a.Position.y))
    //        {
    //            if (node.SubDivisionsCreated)
    //                AddToSubQuad(targetNodeIndex, permaObjectId); // add this to one of the subquads

    //            // still enough space
    //            else if (node.MovableElementsCounter < MaxEntitiesPerQuad)
    //                node.MovableTable[node.MovableElementsCounter++] = permaObjectId;
    //            else
    //            {
    //                // additional subdivision needed
    //                CreateSubdivisions(targetNodeIndex);
    //                AddToSubQuad(targetNodeIndex, permaObjectId); // add this to one of the subquads

    //                while (node.MovableElementsCounter > 0)
    //                    AddToSubQuad(targetNodeIndex, node.MovableTable[--node.MovableElementsCounter]); // go from the end to the start
    //            }

    //            node.RemoveFromPermaTable(i);
    //            continue;
    //        }

    //        i++;
    //    }

    //    i = 0;
    //    while (i < node.MovableElementsCounter)
    //    {
    //        int movaObjectId = node.MovableTable[i];
    //        ref Agent a = ref GameEngine.Agents[movaObjectId];

    //        if (a.Flags > 2) // dead
    //        {
    //            i++;
    //            continue;
    //        }

    //        // are you still in this quad?
    //        if (!node.WithingBoundaries(in node.Boundaries, a.Position))
    //        {
    //            node.RemoveFromMovableTable(i);
    //            AddFromBottom(node.ParentNodeIndex, movaObjectId);
    //            continue;
    //        }

    //        // got to close to the division lines
    //        if (node.TooCloseToDivisionalLines(a.Position.x, a.Position.y))
    //        {
    //            node.AddToPermaTable(movaObjectId);
    //            node.RemoveFromMovableTable(i);
    //            continue;
    //        }

    //        i++;
    //    }

    //    if (node.SubDivisionsCreated)
    //    {
    //        UpdateNode(node.TopLeftNodeIndex);
    //        UpdateNode(node.TopRightNodeIndex);
    //        UpdateNode(node.BottomLeftNodeIndex);
    //        UpdateNode(node.BottomRightNodeIndex);
    //    }
    //}

    //public void CheckCollisions(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    // elements are always sorted by positionX
    //    if (node.PermanentElementsCounter > 0)
    //        CheckCollisionsPermanent(targetNodeIndex);

    //    if (node.SubDivisionsCreated)
    //    {
    //        CheckCollisions(node.TopLeftNodeIndex);
    //        CheckCollisions(node.TopRightNodeIndex);
    //        CheckCollisions(node.BottomLeftNodeIndex);
    //        CheckCollisions(node.BottomRightNodeIndex);
    //    }
    //    else
    //    {
    //        // movable can only be at the bottom of the tree so recursion is not needed
    //        CheckCollisionsMovable(targetNodeIndex);
    //    }
    //}

    //public void CheckCollisionsOnlyThisNode(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    if (node.PermanentElementsCounter > 0)
    //        CheckCollisionsPermanent(targetNodeIndex);

    //    if (!node.SubDivisionsCreated)
    //        CheckCollisionsMovable(targetNodeIndex);
    //}

    ///// <summary>
    ///// This method is faster as it takes advantage of the fact that the passed element's x position 
    ///// is always greater than in all elements in this node.
    ///// </summary>
    //public void CheckCollisionsLeft(int targetNodeIndex, ref Agent a)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    // check a with all perms and movables in this node
    //    // if it contains subdivisions go and call this recursively
    //    if (node.PermanentElementsCounter > 0)
    //    {
    //        for (int i = node.PermanentElementsCounter - 1; i >= 0; i--)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.PermanentTable[i]];
    //            if (b.Flags > 2)
    //                continue;

    //            // if distance becomes too high break the for loop
    //            float difX = a.Position.x - b.Position.x;
    //            // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

    //            if (difX >= GameEngine.AsteroidRadius2)
    //                break; // no collision possible

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                return;
    //        }
    //    }

    //    // check if a is still alive
    //    if (a.Flags < 3 && node.SubDivisionsCreated)
    //    {
    //        CheckCollisionsLeft(node.TopLeftNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisionsLeft(node.TopRightNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisionsLeft(node.BottomLeftNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisionsLeft(node.BottomRightNodeIndex, ref a);
    //    }
    //    else
    //    {
    //        for (int i = node.MovableElementsCounter - 1; i > 0; i--)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.MovableTable[i]];
    //            if (b.Flags > 2)
    //                continue;

    //            // if distance becomes too high break the for loop
    //            float difX = a.Position.x - b.Position.x;
    //            // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

    //            if (difX >= GameEngine.AsteroidRadius2)
    //                break; // no collision possible

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                return;
    //        }
    //    }
    //}

    ///// <summary>
    ///// This method is faster as it takes advantage of the fact that the passed element's x position 
    ///// is always smaller than in all elements in this node.
    ///// </summary>
    //public void CheckCollisionsRight(int targetNodeIndex, ref Agent a)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    // check a with all perms and movables in this node
    //    // if it contains subdivisions go and call this recursively
    //    if (node.PermanentElementsCounter > 0)
    //    {
    //        for (int i = 0; i < node.PermanentElementsCounter; i++)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.PermanentTable[i]];
    //            if (b.Flags > 2)
    //                continue;

    //            // if distance becomes too high break the for loop
    //            float difX = b.Position.x - a.Position.x;
    //            // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

    //            if (difX >= GameEngine.AsteroidRadius2)
    //                break; // no collision possible

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                return;
    //        }
    //    }

    //    // check if a is still alive
    //    if (a.Flags < 3 && node.SubDivisionsCreated)
    //    {
    //        CheckCollisionsRight(node.TopLeftNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisionsRight(node.TopRightNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisionsRight(node.BottomLeftNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisionsRight(node.BottomRightNodeIndex, ref a);
    //    }
    //    else
    //    {
    //        for (int i = 0; i < node.MovableElementsCounter; i++)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.MovableTable[i]];
    //            if (b.Flags > 2)
    //                continue;

    //            // if distance becomes too high break the for loop
    //            float difX = b.Position.x - a.Position.x;
    //            // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

    //            if (difX >= GameEngine.AsteroidRadius2)
    //                break; // no collision possible

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                return;
    //        }
    //    }
    //}

    ///// <summary>
    ///// For cost efficiency this method does not check if the agents are alive.
    ///// It is always the responsibility of the caller function to do that.
    ///// </summary>
    //bool CheckCollisionBetweenAgents(ref Agent a, ref Agent b)
    //{
    //    float difX = b.Position.x - a.Position.x;
    //    if (difX < 0)
    //        difX *= -1;

    //    if (difX >= GameEngine.AsteroidRadius2)
    //        return false; // no collision possible

    //    float difY = b.Position.y - a.Position.y;
    //    if (difY < 0)
    //        difY *= -1;

    //    if (difY >= GameEngine.AsteroidRadius2)
    //        return false; // no collision possible

    //    float distance = FastSqrt(difX * difX + difY * difY);
    //    if (distance >= GameEngine.AsteroidRadius2)
    //        return false; // no collision possible

    //    // collision detected
    //    if (a.Flags == 2) // a is an asteroid
    //    {
    //        a.TimeLeftToRespawn = 1f;
    //        a.Flags = 5; // destroyed asteroid

    //        if (b.Flags == 2) // b is an asteroid
    //        {
    //            b.TimeLeftToRespawn = 1f;
    //            b.Flags = 5;
    //        }
    //        else if (b.Flags == 1) // b is a laser beam
    //        {
    //            b.Flags = 4; // destroyed laser beam
    //            GameEngine.NumberOfAsteroidsDestroyedThisFrame++;
    //        }
    //        else // b is a spaceship
    //        {
    //            b.Flags = 3;
    //            GameEngine.DidPlayerDieThisFrame = true;
    //        }

    //        return true;
    //    }
    //    else if (a.Flags == 1) // a is a laser beam
    //    {
    //        // laser beam cannot collide with anything else than an asteroid
    //        a.Flags = 4; // destroyed laser beam
    //        b.TimeLeftToRespawn = 1f;
    //        b.Flags = 5; // destroyed asteroid
    //        GameEngine.NumberOfAsteroidsDestroyedThisFrame++;

    //        return true;
    //    }
    //    else // a is a player
    //    {
    //        // player cannot collide with anything else than an asteroid
    //        a.Flags = 3; // destroyed spaceship
    //        b.Flags = 5; // destroyed asteroid
    //        GameEngine.DidPlayerDieThisFrame = true;

    //        return true;
    //    }
    //}


    //public void CheckCollisionsPermanent(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    // perform checks for this agent with all the other in the permanent table
    //    for (int i = 0; i < node.PermanentElementsCounter; i++)
    //    {
    //        // check this perm with all other perms
    //        ref Agent a = ref GameEngine.Agents[node.PermanentTable[i]];

    //        if (a.Flags > 2)
    //            continue;

    //        int j = i;
    //        while (++j < node.PermanentElementsCounter)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.PermanentTable[j]];
    //            if (b.Flags > 2)
    //                continue;

    //            // if distance becomes too high break the while loop
    //            float difX = b.Position.x - a.Position.x;

    //            // difX can never be negative in this context because we compare elements from the same table
    //            if (difX >= GameEngine.AsteroidRadius2)
    //                break; // no collision possible

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                break;
    //        }

    //        if (a.Flags > 2)
    //            continue;

    //        // then check this perm with all movables
    //        for (int k = 0; k < node.MovableElementsCounter; k++)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.MovableTable[k]];
    //            if (b.Flags > 2)
    //                continue;

    //            // if distance becomes too high break the for loop
    //            float difX = b.Position.x - a.Position.x;
    //            if (difX < 0)
    //                difX *= -1;

    //            if (difX >= GameEngine.AsteroidRadius2)
    //                break; // no collision possible

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                break;
    //        }

    //        // the agent is still alive and we can continue searching by going down the tree
    //        if (a.Flags < 3 && node.SubDivisionsCreated)
    //        {
    //            // check if this one can collide with any one from any subdivisions

    //            float distanceX = node.DivisionLineX - a.Position.x;
    //            if (distanceX < 0)
    //                distanceX *= -1;

    //            // a is at the divisional line - optimizations can be performed
    //            if (distanceX < GameEngine.AsteroidRadius)
    //            {
    //                CheckCollisionsLeft(node.TopLeftNodeIndex, ref a);
    //                if (a.Flags > 2) // died in the check above
    //                    continue;

    //                CheckCollisionsRight(node.TopRightNodeIndex, ref a);
    //                if (a.Flags > 2) // died in the check above
    //                    continue;

    //                CheckCollisionsLeft(node.BottomLeftNodeIndex, ref a);
    //                if (a.Flags > 2) // died in the check above
    //                    continue;

    //                CheckCollisionsRight(node.BottomRightNodeIndex, ref a);
    //            }
    //            else
    //            {
    //                CheckCollisions(node.TopLeftNodeIndex, ref a);
    //                if (a.Flags > 2) // died in the check above
    //                    continue;

    //                CheckCollisions(node.TopRightNodeIndex, ref a);
    //                if (a.Flags > 2) // died in the check above
    //                    continue;

    //                CheckCollisions(node.BottomLeftNodeIndex, ref a);
    //                if (a.Flags > 2) // died in the check above
    //                    continue;

    //                CheckCollisions(node.BottomRightNodeIndex, ref a);
    //            }
    //        }
    //    }
    //}

    //public void CheckCollisionsMovable(int targetNodeIndex)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    for (int i = 0; i < node.MovableElementsCounter - 1; i++)
    //    {
    //        ref Agent a = ref GameEngine.Agents[node.MovableTable[i]];
    //        if (a.Flags > 2)
    //            continue;

    //        int j = i + 1;
    //        do
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.MovableTable[j]];
    //            if (b.Flags > 2)
    //            {
    //                if (++j < node.MovableElementsCounter)
    //                    continue;
    //                else
    //                    break;
    //            }

    //            // if distance becomes too high break the for loop
    //            float difX = b.Position.x - a.Position.x;

    //            // difX can never be negative in this context because we compare elements from the same table
    //            if (difX >= GameEngine.AsteroidRadius2)
    //                break; // no collision possible

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                break;

    //            j++;
    //        }
    //        while (j < node.MovableElementsCounter);
    //    }
    //}

    //void CheckCollisions(int targetNodeIndex, ref Agent a)
    //{
    //    ref QuadTreeNode node = ref NodeArray[targetNodeIndex];

    //    // check a with all perms and movables in this node
    //    // if it contains subdivisions go and call this recursively
    //    if (node.PermanentElementsCounter > 0)
    //    {
    //        for (int i = 0; i < node.PermanentElementsCounter; i++)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.PermanentTable[i]];
    //            if (b.Flags > 2)
    //                continue;

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                return;
    //        }
    //    }

    //    // check if a is still alive
    //    if (a.Flags < 3 && node.SubDivisionsCreated)
    //    {
    //        CheckCollisions(node.TopLeftNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisions(node.TopRightNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisions(node.BottomLeftNodeIndex, ref a);
    //        if (a.Flags > 2) // died in the check above
    //            return;

    //        CheckCollisions(node.BottomRightNodeIndex, ref a);
    //    }
    //    else
    //    {
    //        for (int i = 0; i < node.MovableElementsCounter; i++)
    //        {
    //            ref Agent b = ref GameEngine.Agents[node.MovableTable[i]];
    //            if (b.Flags > 2)
    //                continue;

    //            if (CheckCollisionBetweenAgents(ref a, ref b))
    //                return;
    //        }
    //    }
    //}

    //#region Fast Sqrt
    //[StructLayout(LayoutKind.Explicit)]
    //struct FloatIntUnion
    //{
    //    [FieldOffset(0)]
    //    public float Flt;

    //    [FieldOffset(0)]
    //    public int Tmp;
    //}

    //// not written by me, I found it on the Internet
    //// it is around 10 - 15% faster than the Mathf.Sqrt from Unity.Mathematics 
    //// (which probably uses the inverse square root method from Quake 3 based on its cost).
    //// but that comes for a cost of less accurate approximation (from 0.5% to 5% less accurate)
    //float FastSqrt(float number)
    //{
    //    if (number == 0)
    //        return 0;

    //    FloatIntUnion u;
    //    u.Tmp = 0;
    //    u.Flt = number;
    //    u.Tmp -= 1 << 23; /* Subtract 2^m. */
    //    u.Tmp >>= 1; /* Divide by 2. */
    //    u.Tmp += 1 << 29; /* Add ((b + 1) / 2) * 2^m. */
    //    return u.Flt;
    //}
    //#endregion
}
