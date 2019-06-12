using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

public class QuadTreeNode
{
    public class AgentComparer : IComparer<int>
    {
        public int Compare(int a, int b) => GameEngine.Agents[a].Position.x >= GameEngine.Agents[b].Position.x ? 1 : -1;
    }

    const float AsteroidRadius = 0.20f;
    const float AsteroidRadius2 = AsteroidRadius + AsteroidRadius;

    static readonly AgentComparer _agentComparer = new AgentComparer();

    public QuadTreeNode TopLeftQuadrant;
    public QuadTreeNode TopRightQuadrant;
    public QuadTreeNode BottomLeftQuadrant;
    public QuadTreeNode BottomRightQuadrant;

    readonly CollisionSystem _root;
    readonly float _divisionLineX, _divisionLineY;
    readonly QuadTreeNode _rootQuadNode;
    readonly int[] _movableTable;
    readonly Quad _boundaries;
    readonly int _currentDepth;
    int[] _permanentTable;
    bool _subDivisionsCreated;
    int _movableElementsCounter;
    int _permanentElementsCounter;

    public QuadTreeNode(CollisionSystem root, QuadTreeNode rootQuadNode, in Quad dimensions, int currentDepth)
    {
        _root = root;
        _rootQuadNode = rootQuadNode;
        _movableTable = new int[root.MaxEntitiesPerQuad];

        _permanentTable = new int[root.InitialPermaTableLength];
        _boundaries = dimensions;
        _currentDepth = currentDepth;
        _divisionLineX = (dimensions.MinX + dimensions.MaxX) / 2;
        _divisionLineY = (dimensions.MinY + dimensions.MaxY) / 2;
    }

    public void Add(int agentId)
    {
        // first check if the element can not be put in subdivision due to its proximity to the division lines
        if (TooCloseToDivisionalLines(GameEngine.Agents[agentId].Position.x, GameEngine.Agents[agentId].Position.y))
        {
            // add to perma list
            _permanentTable[_permanentElementsCounter++] = agentId;

            // resize the table
            if (_permanentElementsCounter == _permanentTable.Length)
            {
                int[] tempTable = new int[_permanentElementsCounter];
                Buffer.BlockCopy(_permanentTable, 0, tempTable, 0, _permanentElementsCounter * 4);

                _permanentTable = new int[_permanentElementsCounter * 2];
                Buffer.BlockCopy(tempTable, 0, _permanentTable, 0, _permanentElementsCounter * 4);
            }

            return;
        }

        // was already subdivided
        // during subdivision we always create all quads so here just check if at least is not null to proceed
        if (_subDivisionsCreated)
        {
            AddToSubQuad(agentId); // recursive call
            return; // agent added to one of the subquads
        }

        if (_movableElementsCounter < _root.MaxEntitiesPerQuad)
        {
            _movableTable[_movableElementsCounter++] = agentId;

            return; // agent added to this quad
        }
        else
        {
            // additional subdivision needed
            CreateSubdivisions();
            AddToSubQuad(agentId); // add this to one of the subquads

            while (_movableElementsCounter > 0)
            {
                // go from the end to the start
                AddToSubQuad(_movableTable[--_movableElementsCounter]);
            }
        }
    }

    public void CreateSubdivisions()
    {
        _subDivisionsCreated = true;

        // first subdivision goes differently as we use both negative and positive indexes
        TopLeftQuadrant = new QuadTreeNode(
            _root,
            this,
            new Quad(
                _boundaries.MinX,
                _boundaries.MaxX - (_boundaries.MaxX - _boundaries.MinX) / 2,
                _boundaries.MaxY - (_boundaries.MaxY - _boundaries.MinY) / 2,
                _boundaries.MaxY),
            _currentDepth + 1);

        TopRightQuadrant = new QuadTreeNode(
            _root,
            this,
            new Quad(
                _boundaries.MaxX - (_boundaries.MaxX - _boundaries.MinX) / 2,
                _boundaries.MaxX,
                _boundaries.MaxY - (_boundaries.MaxY - _boundaries.MinY) / 2,
                _boundaries.MaxY),
            _currentDepth + 1);

        BottomLeftQuadrant = new QuadTreeNode(
            _root,
            this,
            new Quad(
                _boundaries.MinX,
                _boundaries.MaxX - (_boundaries.MaxX - _boundaries.MinX) / 2,
                _boundaries.MinY,
                _boundaries.MaxY - (_boundaries.MaxY - _boundaries.MinY) / 2),
            _currentDepth + 1);

        BottomRightQuadrant = new QuadTreeNode(
            _root,
            this,
            new Quad(
                _boundaries.MaxX - (_boundaries.MaxX - _boundaries.MinX) / 2,
                _boundaries.MaxX,
                _boundaries.MinY,
                _boundaries.MaxY - (_boundaries.MaxY - _boundaries.MinY) / 2),
            _currentDepth + 1);
    }

    /// <summary>
    /// Updates all agents in terms of theirs position withing the quadtree structure.
    /// Additionally removes those elements that are marked as dead.
    /// Dead elements will be reinserted when necessary by other functions.
    /// </summary>
    public void UpdateNode()
    {
        int i = 0;
        while (i < _permanentElementsCounter)
        {
            int permaObjectId = _permanentTable[i];
            ref Agent a = ref GameEngine.Agents[permaObjectId];

            if (a.Flags > 2) // dead
            {
                i++;
                continue;
            }

            // no longer in the quad
            if (!WithingQuadArea(in _boundaries, ref a))
            {
                // remove from perm by inserting the last one on current spot
                if (i < --_permanentElementsCounter)
                    _permanentTable[i] = _permanentTable[_permanentElementsCounter];

                _rootQuadNode.AddFromBottom(permaObjectId);
                continue;
            }

            // got out of the division lines
            if (!TooCloseToDivisionalLines(a.Position.x, a.Position.y))
            {
                if (_subDivisionsCreated)
                    AddToSubQuad(permaObjectId); // add this to one of the subquads

                // still enough space
                else if (_movableElementsCounter < _root.MaxEntitiesPerQuad)
                    _movableTable[_movableElementsCounter++] = permaObjectId;
                else
                {
                    // additional subdivision needed
                    CreateSubdivisions();
                    AddToSubQuad(permaObjectId); // add this to one of the subquads

                    while (_movableElementsCounter > 0)
                        AddToSubQuad(_movableTable[--_movableElementsCounter]); // go from the end to the start
                }

                // if this is the last one simply decrease the counter
                // otherwise put the last one's value in the i's spot and decrease the counter by 1
                if (i < --_permanentElementsCounter)
                    _permanentTable[i] = _permanentTable[_permanentElementsCounter];

                continue;
            }

            i++;
        }

        i = 0;
        while (i < _movableElementsCounter)
        {
            int movaObjectId = _movableTable[i];
            ref Agent a = ref GameEngine.Agents[movaObjectId];

            if (a.Flags > 2) // dead
            {
                i++;
                continue;
            }

            // are you still in this quad?
            if (!WithingQuadArea(in _boundaries, ref a))
            {
                // if this is the last one simply decrease the counter
                // otherwise put the last one's value in the i's spot and decrease the counter by 1
                if (i < --_movableElementsCounter)
                    _movableTable[i] = _movableTable[_movableElementsCounter];

                _rootQuadNode.AddFromBottom(movaObjectId);
                continue;
            }

            // got to close to the division lines
            if (TooCloseToDivisionalLines(a.Position.x, a.Position.y))
            {
                // add to perma list
                _permanentTable[_permanentElementsCounter++] = movaObjectId;

                // resize the table if necessary
                if (_permanentElementsCounter == _permanentTable.Length)
                {
                    // extend the table two times
                    int[] tempTable = new int[_permanentElementsCounter];
                    Buffer.BlockCopy(_permanentTable, 0, tempTable, 0, _permanentElementsCounter * 4);

                    _permanentTable = new int[_permanentElementsCounter * 2];
                    Buffer.BlockCopy(tempTable, 0, _permanentTable, 0, _permanentElementsCounter * 4);
                }

                // if this is the last one simply decrease the counter
                // otherwise put the last one's value in the i's spot and decrease the counter by 1
                if (i < --_movableElementsCounter)
                    _movableTable[i] = _movableTable[_movableElementsCounter];

                continue;
            }

            i++;
        }

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.UpdateNode();
            TopRightQuadrant.UpdateNode();
            BottomLeftQuadrant.UpdateNode();
            BottomRightQuadrant.UpdateNode();
        }
    }

    /// <summary>
    /// Sort all elements in all tables by their position X in this node and 
    /// in all subsequent nodes.
    /// </summary>
    public void SortElements()
    {
        if (_permanentElementsCounter > 1)
            Array.Sort(_permanentTable, 0, _permanentElementsCounter, _agentComparer);

        if (_movableElementsCounter > 1)
            Array.Sort(_movableTable, 0, _movableElementsCounter, _agentComparer);

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.SortElements();
            TopRightQuadrant.SortElements();
            BottomLeftQuadrant.SortElements();
            BottomRightQuadrant.SortElements();
        }
    }

    /// <summary>
    /// Sort all elements in all tables by their position X.
    /// </summary>
    public void SortElementsOnlyThisNode()
    {
        if (_permanentElementsCounter > 1)
            Array.Sort(_permanentTable, 0, _permanentElementsCounter, _agentComparer);

        if (_movableElementsCounter > 1)
            Array.Sort(_movableTable, 0, _movableElementsCounter, _agentComparer);
    }

    public void RemoveDeadElements()
    {
        int i = 0;
        while (i < _permanentElementsCounter)
        {
            if (GameEngine.Agents[_permanentTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --_permanentElementsCounter)
                    _permanentTable[i] = _permanentTable[_permanentElementsCounter];
                continue;
            }
            i++;
        }

        i = 0;
        while (i < _movableElementsCounter)
        {
            if (GameEngine.Agents[_movableTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --_movableElementsCounter)
                    _movableTable[i] = _movableTable[_movableElementsCounter];
                continue;
            }
            i++;
        }

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.RemoveDeadElements();
            TopRightQuadrant.RemoveDeadElements();
            BottomLeftQuadrant.RemoveDeadElements();
            BottomRightQuadrant.RemoveDeadElements();
        }
    }

    public void RemoveDeadElementsOnlyThisNode()
    {
        int i = 0;
        while (i < _permanentElementsCounter)
        {
            if (GameEngine.Agents[_permanentTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --_permanentElementsCounter)
                    _permanentTable[i] = _permanentTable[_permanentElementsCounter];
                continue;
            }
            i++;
        }

        i = 0;
        while (i < _movableElementsCounter)
        {
            if (GameEngine.Agents[_movableTable[i]].Flags > 2) // dead
            {
                // remove from the tree structure
                if (i < --_movableElementsCounter)
                    _movableTable[i] = _movableTable[_movableElementsCounter];
                continue;
            }
            i++;
        }
    }

    public void CheckCollisions()
    {
        // elements are always sorted by positionX
        if (_permanentElementsCounter > 0)
            CheckCollisionsPermanent();

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.CheckCollisions();
            TopRightQuadrant.CheckCollisions();
            BottomLeftQuadrant.CheckCollisions();
            BottomRightQuadrant.CheckCollisions();
        }
        else
        {
            // movable can only be at the bottom of the tree so recursion is not needed
            CheckCollisionsMovable();
        }
    }

    public void CheckCollisionsOnlyThisNode()
    {
        if (_permanentElementsCounter > 0)
            CheckCollisionsPermanent();

        if (!_subDivisionsCreated)
            CheckCollisionsMovable();
    }

    /// <summary>
    /// Used to move an agent upwards in the hierarchy.
    /// This method calls itself recursively until it founds a suitable node to put the agent into.
    /// </summary>
    void AddFromBottom(int agentId)
    {
        if (WithingQuadArea(in _boundaries, ref GameEngine.Agents[agentId]))
            Add(agentId);
        else
            _rootQuadNode.AddFromBottom(agentId);
    }

    /// <summary>
    /// Returns true if the agent with these coordinates is to close to division lines 
    /// to be able to be put deeper in the hierarchy, otherwise false.
    /// </summary>
    bool TooCloseToDivisionalLines(float agentPosX, float agentPosY)
    {
        float distanceX = _divisionLineX - agentPosX;
        if (distanceX < 0)
            distanceX *= -1;

        if (distanceX < AsteroidRadius)
            return true;

        float distanceY = _divisionLineY - agentPosY;
        if (distanceY < 0)
            distanceY *= -1;

        return distanceY < AsteroidRadius;
    }

    void AddToSubQuad(int id)
    {
        float2 pos = GameEngine.Agents[id].Position;

        if (pos.x >= _divisionLineX)
        {
            // right side
            if (pos.y >= _divisionLineY)
                TopRightQuadrant.Add(id);
            else
                BottomRightQuadrant.Add(id);
        }
        else
        {
            // left side
            if (pos.y >= _divisionLineY)
                TopLeftQuadrant.Add(id);
            else
                BottomLeftQuadrant.Add(id);
        }
    }

    bool WithingQuadArea(in Quad rect, ref Agent a) =>
        a.Position.x >= rect.MinX
        && a.Position.x <= rect.MaxX
        && a.Position.y >= rect.MinY
        && a.Position.y <= rect.MaxY;

    void CheckCollisionsPermanent()
    {
        // perform checks for this agent with all the other in the permanent table
        for (int i = 0; i < _permanentElementsCounter; i++)
        {
            // check this perm with all other perms
            ref Agent a = ref GameEngine.Agents[_permanentTable[i]];

            if (a.Flags > 2)
                continue;

            int j = i;
            while (++j < _permanentElementsCounter)
            {
                ref Agent b = ref GameEngine.Agents[_permanentTable[j]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the while loop
                float difX = b.Position.x - a.Position.x;

                // difX can never be negative in this context because we compare elements from the same table
                if (difX >= AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    break;
            }

            if (a.Flags > 2)
                continue;

            // then check this perm with all movables
            for (int k = 0; k < _movableElementsCounter; k++)
            {
                ref Agent b = ref GameEngine.Agents[_movableTable[k]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;
                if (difX < 0)
                    difX *= -1;

                if (difX >= AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    break;
            }

            // the agent is still alive and we can continue searching by going down the tree
            if (a.Flags < 3 && _subDivisionsCreated)
            {
                // check if this one can collide with any one from any subdivisions

                float distanceX = _divisionLineX - a.Position.x;
                if (distanceX < 0)
                    distanceX *= -1;

                // a is at the divisional line - optimizations can be performed
                if (distanceX < AsteroidRadius)
                {
                    TopLeftQuadrant.CheckCollisionsLeft(ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    TopRightQuadrant.CheckCollisionsRight(ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    BottomLeftQuadrant.CheckCollisionsLeft(ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    BottomRightQuadrant.CheckCollisionsRight(ref a);
                }
                else
                {
                    TopLeftQuadrant.CheckCollisions(ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    TopRightQuadrant.CheckCollisions(ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    BottomLeftQuadrant.CheckCollisions(ref a);
                    if (a.Flags > 2) // died in the check above
                        continue;

                    BottomRightQuadrant.CheckCollisions(ref a);
                }
            }
        }
    }

    void CheckCollisionsMovable()
    {
        for (int i = 0; i < _movableElementsCounter - 1; i++)
        {
            ref Agent a = ref GameEngine.Agents[_movableTable[i]];
            if (a.Flags > 2)
                continue;

            int j = i + 1;
            do
            {
                ref Agent b = ref GameEngine.Agents[_movableTable[j]];
                if (b.Flags > 2)
                {
                    if (++j < _movableElementsCounter)
                        continue;
                    else
                        break;
                }

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;

                // difX can never be negative in this context because we compare elements from the same table
                if (difX >= AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    break;

                j++;
            }
            while (j < _movableElementsCounter);
        }
    }

    void CheckCollisions(ref Agent a)
    {
        // check a with all perms and movables in this node
        // if it contains subdivisions go and call this recursively
        if (_permanentElementsCounter > 0)
        {
            for (int i = 0; i < _permanentElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[_permanentTable[i]];
                if (b.Flags > 2)
                    continue;

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }

        // check if a is still alive
        if (a.Flags < 3 && _subDivisionsCreated)
        {
            TopLeftQuadrant.CheckCollisions(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            TopRightQuadrant.CheckCollisions(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            BottomLeftQuadrant.CheckCollisions(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            BottomRightQuadrant.CheckCollisions(ref a);
        }
        else
        {
            for (int i = 0; i < _movableElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[_movableTable[i]];
                if (b.Flags > 2)
                    continue;

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }
    }

    /// <summary>
    /// This method is faster as it takes advantage of the fact that the passed element's x position 
    /// is always greater than in all elements in this node.
    /// </summary>
    void CheckCollisionsLeft(ref Agent a)
    {
        // check a with all perms and movables in this node
        // if it contains subdivisions go and call this recursively
        if (_permanentElementsCounter > 0)
        {
            for (int i = _permanentElementsCounter - 1; i >= 0; i--)
            {
                ref Agent b = ref GameEngine.Agents[_permanentTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = a.Position.x - b.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }

        // check if a is still alive
        if (a.Flags < 3 && _subDivisionsCreated)
        {
            TopLeftQuadrant.CheckCollisionsLeft(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            TopRightQuadrant.CheckCollisionsLeft(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            BottomLeftQuadrant.CheckCollisionsLeft(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            BottomRightQuadrant.CheckCollisionsLeft(ref a);
        }
        else
        {
            for (int i = _movableElementsCounter - 1; i > 0; i--)
            {
                ref Agent b = ref GameEngine.Agents[_movableTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = a.Position.x - b.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= AsteroidRadius2)
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
    void CheckCollisionsRight(ref Agent a)
    {
        // check a with all perms and movables in this node
        // if it contains subdivisions go and call this recursively
        if (_permanentElementsCounter > 0)
        {
            for (int i = 0; i < _permanentElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[_permanentTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= AsteroidRadius2)
                    break; // no collision possible

                if (CheckCollisionBetweenAgents(ref a, ref b))
                    return;
            }
        }

        // check if a is still alive
        if (a.Flags < 3 && _subDivisionsCreated)
        {
            TopLeftQuadrant.CheckCollisionsRight(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            TopRightQuadrant.CheckCollisionsRight(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            BottomLeftQuadrant.CheckCollisionsRight(ref a);
            if (a.Flags > 2) // died in the check above
                return;

            BottomRightQuadrant.CheckCollisionsRight(ref a);
        }
        else
        {
            for (int i = 0; i < _movableElementsCounter; i++)
            {
                ref Agent b = ref GameEngine.Agents[_movableTable[i]];
                if (b.Flags > 2)
                    continue;

                // if distance becomes too high break the for loop
                float difX = b.Position.x - a.Position.x;
                // difX may sometimes be negative here due to slight number misrepresentation but it doesn't rly matter tho

                if (difX >= AsteroidRadius2)
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

        if (difX >= AsteroidRadius2)
            return false; // no collision possible

        float difY = b.Position.y - a.Position.y;
        if (difY < 0)
            difY *= -1;

        if (difY >= AsteroidRadius2)
            return false; // no collision possible

        float distance = FastSqrt(difX * difX + difY * difY);
        if (distance >= AsteroidRadius2)
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
                GameEngine.DidPlayerDiedThisFrame = true;
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
            GameEngine.DidPlayerDiedThisFrame = true;

            return true;
        }
    }

    //public void MergeEmptyNodes()
    //{
    //    if(SubDivisionsCreated)
    //    {
    //        var howMany = _permanentList.Count 
    //            + _movableList.Count
    //            + TopLeftQuadrant._permanentList.Count
    //            + TopLeftQuadrant._movableList.Count
    //            + TopRightQuadrant._permanentList.Count
    //            + TopRightQuadrant._movableList.Count
    //            + BottomLeftQuadrant._permanentList.Count
    //            + BottomLeftQuadrant._movableList.Count
    //            + BottomRightQuadrant._permanentList.Count
    //            + BottomRightQuadrant._movableList.Count;

    //        if(howMany < 5)
    //        {
    //            // merge the sub quads
    //            for (int i = 0; i < TopLeftQuadrant._permanentList.Count; i++)
    //                Add(TopLeftQuadrant._permanentList[i]);

    //            for (int i = 0; i < TopLeftQuadrant._movableList.Count; i++)
    //                Add(TopLeftQuadrant._movableList[i]);

    //            for (int i = 0; i < TopRightQuadrant._permanentList.Count; i++)
    //                Add(TopRightQuadrant._permanentList[i]);

    //            for (int i = 0; i < TopRightQuadrant._movableList.Count; i++)
    //                Add(TopRightQuadrant._movableList[i]);

    //            for (int i = 0; i < BottomLeftQuadrant._permanentList.Count; i++)
    //                Add(BottomLeftQuadrant._permanentList[i]);

    //            for (int i = 0; i < BottomLeftQuadrant._movableList.Count; i++)
    //                Add(BottomLeftQuadrant._movableList[i]);

    //            for (int i = 0; i < BottomRightQuadrant._permanentList.Count; i++)
    //                Add(BottomRightQuadrant._permanentList[i]);

    //            for (int i = 0; i < BottomRightQuadrant._movableList.Count; i++)
    //                Add(BottomRightQuadrant._movableList[i]);

    //            TopLeftQuadrant = null;
    //            TopRightQuadrant = null;
    //            BottomLeftQuadrant = null;
    //            BottomRightQuadrant = null;
    //            SubDivisionsCreated = false;

    //            return;
    //        }

    //        TopLeftQuadrant.MergeNodes();
    //        TopRightQuadrant.MergeNodes();
    //        BottomLeftQuadrant.MergeNodes();
    //        BottomRightQuadrant.MergeNodes();
    //    }
    //}

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

    #region Assertions
    public void NoItermidiateMovablesCheck()
    {
        //if (_movableList.Count > 0 && SubDivisionsCreated)
        if (_movableElementsCounter > 0 && _subDivisionsCreated)
            Debug.LogError($"The quadnode at depth level {_currentDepth} contains movable elements despite having subdivisions.");

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.NoItermidiateMovablesCheck();
            TopRightQuadrant.NoItermidiateMovablesCheck();
            BottomLeftQuadrant.NoItermidiateMovablesCheck();
            BottomRightQuadrant.NoItermidiateMovablesCheck();
        }
    }

    public void NoDeadAgentsCheck()
    {
        for (int i = 0; i < _permanentElementsCounter; i++)
            if (GameEngine.Agents[_permanentTable[i]].Flags > 2)
                Debug.LogError($"Dead element at depth level {_currentDepth} in permanent table id={_permanentTable[i]}.");

        for (int i = 0; i < _movableElementsCounter; i++)
            if (GameEngine.Agents[_movableTable[i]].Flags > 2)
                Debug.LogError($"Dead element at depth level {_currentDepth} in movable table id={_movableTable[i]}.");

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.NoDeadAgentsCheck();
            TopRightQuadrant.NoDeadAgentsCheck();
            BottomLeftQuadrant.NoDeadAgentsCheck();
            BottomRightQuadrant.NoDeadAgentsCheck();
        }
    }

    public void AgentsNumberCoherencyCheck()
    {
        int agentsNumberTable = 0;

        for (int i = 0; i < GameEngine.Agents.Length; i++)
            if (GameEngine.Agents[i].Flags < 3)
                agentsNumberTable++;

        int permanentAgentsNumberInTree = 0;
        int movableAgentsNumberInTree = 0;

        AgentsNumberCoherencyCheckInternal(ref permanentAgentsNumberInTree, ref movableAgentsNumberInTree);

        if (agentsNumberTable != permanentAgentsNumberInTree + movableAgentsNumberInTree)
            Debug.LogError($"The number of live agents in the global table ({agentsNumberTable}) is different " +
                $"than in the quadtree (perma:{permanentAgentsNumberInTree}, mov:{movableAgentsNumberInTree}).");
    }

    void AgentsNumberCoherencyCheckInternal(ref int permaAgentsNumberInTree, ref int movableAgentsNumberInTree)
    {
        permaAgentsNumberInTree += _permanentElementsCounter;
        movableAgentsNumberInTree += _movableElementsCounter;

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
            TopRightQuadrant.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
            BottomLeftQuadrant.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
            BottomRightQuadrant.AgentsNumberCoherencyCheckInternal(ref permaAgentsNumberInTree, ref movableAgentsNumberInTree);
        }
    }

    public void AgentsOrderCheck()
    {
        for (int i = 0; i < _permanentElementsCounter - 1; i++)
            if (GameEngine.Agents[_permanentTable[i]].Position.x > GameEngine.Agents[_permanentTable[i + 1]].Position.x)
                Debug.LogError("Agents are not in order! Sort function fail.");

        for (int i = 0; i < _movableElementsCounter - 1; i++)
            if (GameEngine.Agents[_movableTable[i]].Position.x > GameEngine.Agents[_movableTable[i + 1]].Position.x)
                Debug.LogError("Agents are not in order! Sort function fail.");

        if (_subDivisionsCreated)
        {
            TopLeftQuadrant.AgentsOrderCheck();
            TopRightQuadrant.AgentsOrderCheck();
            BottomLeftQuadrant.AgentsOrderCheck();
            BottomRightQuadrant.AgentsOrderCheck();
        }
    }
    #endregion
}
