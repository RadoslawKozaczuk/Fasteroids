using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class GameEngine : MonoBehaviour
{
    #region Constants
    public const float AsteroidRadius = 0.19f;
    public const float AsteroidRadius2 = AsteroidRadius + AsteroidRadius;
    const float AsteroidTranformValueZ = 0.4f;
    const float LaserRadius = 0.07f;
    const float LaserSpeed = 2.5f;

    const float AsteroidSpeedMin = 0.009f; // distance traveled per frame
    const float AsteroidSpeedMax = 0.02f; // distance traveled per frame
    const int GridDimensionInt = 240;
    const float GridDimensionFloat = 240;
    const int TotalNumberOfAsteroids = GridDimensionInt * GridDimensionInt;

    const float PlayerRadius = 0.08f;
    public const float PlayerRotationFactor = 3f;
    public const float PlayerSpeed = 1.25f;

    const float AsteroidPlayerRadius = AsteroidRadius + PlayerRadius; // to reduce number of additions
    const float AsteroidLaserRadius = AsteroidRadius + LaserRadius; // to reduce number of additions

    const float WorldOffetValue = 1000f; // we move world top right to be sure we operate on positive numbers

    // pool sizes
    const int AsteroidPoolSize = 40; // in tests this never went above 35 so for safety i gave 5 more
    const int LaserPoolSize = 6; // we shot once per 0.5 frame and the lifetime is 3s

    const float FrustumSizeX = 3.8f;
    const float FrustumSizeY = 2.3f;
    #endregion

    //public static readonly Agent[] Agents = new Agent[1 + 6 + TotalNumberOfAsteroids]; // player + total number of lasers

    public static int NumberOfAsteroidsDestroyedThisFrame;
    public static bool DidPlayerDieThisFrame;

    #region Private Fields
    // readonly fields and tables
    static readonly float[] _speedLookupTable = new float[256];
    static readonly float[] _directionLookupTable = new float[256];

    // object pools
    static readonly GameObject[] _laserPool = new GameObject[LaserPoolSize];

    // prefabs
    [SerializeField] GameObject _asteroidPrefab;
    [SerializeField] GameObject _laserBeamPrefab;
    [SerializeField] GameObject _spaceshipPrefab;

    // other references
    [SerializeField] Text _playerScoreLabel;
    [SerializeField] Camera _mainCamera;
    [SerializeField] Button _restartButton;
    [SerializeField] Text _youLoseLabel;

    CollisionSystem _collisionSystem;
    Transform _playerTransform;
    float _timeToFireNextLaser = 0.5f;
    int _laserNextFreeIndex;
    int _playerScore = 0;
    bool _playerDestroyed;
    int _asteroidPoolLastUsedObjectId;
    #endregion

    // ECS related
    EntityManager _entityManager;
    [SerializeField] Mesh _mesh;
    [SerializeField] Material _spaceshipMaterial;
    [SerializeField] Material _laserBeamMaterial;
    [SerializeField] Material _asteroidMaterial;
    NativeArray<Entity> _spaceshipArray;
    NativeArray<Entity> _laserBeamArray;
    NativeArray<Entity> _asteroidArray;

    // tag component
    public struct Spaceship : IComponentData { }
    public struct LaserBeam : IComponentData { }
    public struct Asteroid : IComponentData { }

    public struct MoveSpeed : IComponentData
    {
        public float DirectionX;
        public float DirectionY;
        public float Speed;
    }

    //public struct DeadData : IComponentData
    //{

    //}


    public static NativeList<float> RespawnList;

    void Start()
    {
        _entityManager = World.Active.EntityManager;

        EntityArchetype asteroidArchetyoe = _entityManager.CreateArchetype(
            typeof(RenderMesh), 
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(MoveSpeed),
            typeof(Asteroid));

        EntityArchetype spaceshipArchetyoe = _entityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(Rotation),
            typeof(Spaceship)
        //    typeof(PlayerInputData)
        );

        _spaceshipArray = new NativeArray<Entity>(1, Allocator.Persistent);
        //_laserBeamArray = new NativeArray<Entity>(6, Allocator.Persistent);
        _asteroidArray = new NativeArray<Entity>(TotalNumberOfAsteroids, Allocator.Persistent);

        _entityManager.CreateEntity(asteroidArchetyoe, _asteroidArray); // fill the table with entities

        // initialize asteroids
        int i = 0;
        for (int x = (int)WorldOffetValue; x < GridDimensionInt + WorldOffetValue; x++)
            for (int y = (int)WorldOffetValue; y < GridDimensionInt + WorldOffetValue; y++)
            {
                Entity entity = _asteroidArray[i++];
                _entityManager.SetSharedComponentData(
                    entity,
                    new RenderMesh
                    {
                        mesh = _mesh,
                        material = _asteroidMaterial
                    });

                _entityManager.SetComponentData(
                    entity,
                    new Translation { Value = new float3(x, y, 3f) });

                _entityManager.SetComponentData(
                    entity,
                    new Scale { Value = 0.6f });

                _entityManager.SetComponentData(
                    entity,
                    new MoveSpeed
                    {
                        DirectionX = UnityEngine.Random.Range(-1f, 1f),
                        DirectionY = UnityEngine.Random.Range(-1f, 1f),
                        Speed = UnityEngine.Random.Range(0.05f, 0.2f)
                    });
            }

        _entityManager.CreateEntity(spaceshipArchetyoe, _spaceshipArray); // fill the table with entities

        // initialize spaceship
        Entity spaceship = _spaceshipArray[0];
        _entityManager.SetSharedComponentData(
            spaceship,
            new RenderMesh
            {
                mesh = _mesh,
                material = _spaceshipMaterial
            });

        _entityManager.SetComponentData(
            spaceship,
            new Translation
            {
                Value = new float3(
                    GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
                    GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
                    3f)
            });

        _entityManager.SetComponentData(
            spaceship,
            new Scale { Value = 0.3f });

        _entityManager.SetComponentData(
            spaceship,
            new Rotation { Value = quaternion.RotateZ(0) });

        _playerScoreLabel.text = "score: 0";
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);

        _playerTransform = Instantiate(_spaceshipPrefab).transform;

        _playerTransform.position = new Vector3(
            GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
            GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
            0.3f);

        //_collisionSystem = new CollisionSystem(
        //    new Quad(
        //        500 + GridDimensionFloat / 2,
        //        1_500 + GridDimensionFloat / 2,
        //        500 + GridDimensionFloat / 2,
        //        1_500 + GridDimensionFloat / 2),
        //    20,
        //    10,
        //    5);

        //_collisionSystem.AddElementsToQuadTree(Agents);
    }

    // test related
    //System.Diagnostics.Stopwatch _swUpdate = new System.Diagnostics.Stopwatch();
    //System.Diagnostics.Stopwatch _swUpdateTree = new System.Diagnostics.Stopwatch();
    //System.Diagnostics.Stopwatch _swCollisions = new System.Diagnostics.Stopwatch();
    //System.Diagnostics.Stopwatch _swVisible = new System.Diagnostics.Stopwatch();
    //long _framesPassed = 0;
    bool _testFlag = false;

    void Update()
    {
        // Transform.position is an accessor and calling it results in a calculation behind the scenes
        // so we cache it for the time of the frame calculation

        Vector3 v3 = _playerTransform.position;
        //Agents[0].Position = new float2(v3.x, v3.y);
        //for (int i = 0; i < LaserPoolSize; i++)
        //{
        //    v3 = _laserPool[i].transform.position;
        //    Agents[i + 1].Position = new float2(v3.x, v3.y);
        //}

        HandleInput();

        #region Production Code
        //UpdateAsteroids(); // this also reinserts newly spawned asteroids into the quedtree
        //UpdateLasers(); // this also reinserts newly spawned laser beams into the quadtree
        //_collisionSystem.UpdateNode(0); // this does not remove dead elements

        //// sort elements and check collision in the root node - removing dead elements is done later on
        //_collisionSystem.SortElementsOnlyThisNode(0);
        //_collisionSystem.CheckCollisionsOnlyThisNode(0);

        //QuadTreeNode rootNode = CollisionSystem.NodeArray[0];
        //Task t1 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.SortElements(rootNode.TopLeftNodeIndex);
        //    _collisionSystem.CheckCollisions(rootNode.TopLeftNodeIndex);
        //    _collisionSystem.RemoveDeadElements(rootNode.TopLeftNodeIndex);
        //});

        //Task t2 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.SortElements(rootNode.TopRightNodeIndex);
        //    _collisionSystem.CheckCollisions(rootNode.TopRightNodeIndex);
        //    _collisionSystem.RemoveDeadElements(rootNode.TopRightNodeIndex);
        //});

        //Task t3 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.SortElements(rootNode.BottomLeftNodeIndex);
        //    _collisionSystem.CheckCollisions(rootNode.BottomLeftNodeIndex);
        //    _collisionSystem.RemoveDeadElements(rootNode.BottomLeftNodeIndex);
        //});

        //Task t4 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.SortElements(rootNode.BottomRightNodeIndex);
        //    _collisionSystem.CheckCollisions(rootNode.BottomRightNodeIndex);
        //    _collisionSystem.RemoveDeadElements(rootNode.BottomRightNodeIndex);
        //});

        //// when we have a free core clean up the root node
        //Task.WaitAny(t1, t2, t3, t4);
        //Task t5 = Task.Factory.StartNew(() => _collisionSystem.RemoveDeadElementsOnlyThisNode(0));

        //// wait for all to move forward
        //Task.WaitAll(t1, t2, t3, t4, t5);

        //ShowVisibleAsteroids();
        #endregion

        #region Performance Test Code
        //if (_framesPassed > 100) _swUpdate.Start();
        //UpdateAsteroids();
        //UpdateLasers();
        //if (_framesPassed > 100) _swUpdate.Stop();

        //if (_framesPassed > 100) _swUpdateTree.Start();
        //_collisionSystem.UpdateTreeStructure();
        //if (_framesPassed > 100) _swUpdateTree.Stop();

        //if (_framesPassed > 100) _swCollisions.Start();
        //_collisionSystem.RootNode.SortElementsOnlyThisNode();
        //_collisionSystem.RootNode.CheckCollisionsOnlyThisNode();

        //Task t1 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.RootNode.TopLeftQuadrant.SortElements();
        //    _collisionSystem.RootNode.TopLeftQuadrant.CheckCollisions();
        //    _collisionSystem.RootNode.TopLeftQuadrant.RemoveDeadElements();
        //});

        //Task t2 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.RootNode.TopRightQuadrant.SortElements();
        //    _collisionSystem.RootNode.TopRightQuadrant.CheckCollisions();
        //    _collisionSystem.RootNode.TopRightQuadrant.RemoveDeadElements();
        //});

        //Task t3 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.RootNode.BottomLeftQuadrant.SortElements();
        //    _collisionSystem.RootNode.BottomLeftQuadrant.CheckCollisions();
        //    _collisionSystem.RootNode.BottomLeftQuadrant.RemoveDeadElements();
        //});

        //Task t4 = Task.Factory.StartNew(() =>
        //{
        //    _collisionSystem.RootNode.BottomRightQuadrant.SortElements();
        //    _collisionSystem.RootNode.BottomRightQuadrant.CheckCollisions();
        //    _collisionSystem.RootNode.BottomRightQuadrant.RemoveDeadElements();
        //});

        //// if anyone finish give cleaning the root node task
        //Task.WaitAny(t1, t2, t3, t4);
        //Task t5 = Task.Factory.StartNew(() => _collisionSystem.RootNode.RemoveDeadElementsOnlyThisNode());

        //// wait for all to finish
        //Task.WaitAll(t1, t2, t3, t4, t5);
        //if (_framesPassed > 100) _swCollisions.Stop();

        //// assert: no dead ones in the hierarchy at this point
        //_collisionSystem.RootNode.NoDeadAgentsCheck();
        //// assert: intermediate nodes has their movable tables empty
        //_collisionSystem.RootNode.NoItermidiateMovablesCheck();
        //// assert: the number of live agents in the global table and in the hierarchy are equal
        //_collisionSystem.RootNode.AgentsNumberCoherencyCheck();

        //if (_framesPassed > 100) _swVisible.Start();
        //ShowVisibleAsteroids();
        //if (_framesPassed > 100) _swVisible.Stop();

        //_framesPassed++;
        //if (_testFlag)
        //{
        //    _testFlag = false;

        //    long avgTicksUpdate = _swUpdate.ElapsedTicks / (_framesPassed - 100);
        //    long avgTicksUpdateTree = _swUpdateTree.ElapsedTicks / (_framesPassed - 100);
        //    long avgTicksCollisions = _swCollisions.ElapsedTicks / (_framesPassed - 100);
        //    long avgTickVisible = _swVisible.ElapsedTicks / (_framesPassed - 100);
        //    long totalTicks = avgTicksUpdate + avgTicksUpdateTree + avgTicksCollisions + avgTickVisible;

        //    System.Diagnostics.Debugger.Break();
        //}
        #endregion

        //// handling multi threaded output
        //if (NumberOfAsteroidsDestroyedThisFrame > 0)
        //{
        //    _playerScore += NumberOfAsteroidsDestroyedThisFrame;
        //    NumberOfAsteroidsDestroyedThisFrame = 0;
        //    _playerScoreLabel.text = $"score: {_playerScore}";
        //}

        if (DidPlayerDieThisFrame)
        {
            //GameOver();
            DidPlayerDieThisFrame = false;
        }

        // after calculation is done we can assign it back to the transform
        //float2 playerPosition = Agents[0].Position;
        //_playerTransform.position = new Vector3(playerPosition.x, playerPosition.y, 0.3f);
        //for (int i = 0; i < LaserPoolSize; i++)
        //{
        //    float2 pos = Agents[i + 1].Position;
        //    _laserPool[i].transform.position = new Vector3(pos.x, pos.y, 0.3f);
        //}

        if (!_playerDestroyed)
            _mainCamera.transform.position = _playerTransform.position;
    }

    void HandleInput()
    {
        if (!_playerDestroyed)
        {
            //ref Agent player = ref Agents[0];
            //bool movingBackwards = false;

            //if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            //{
            //    Vector3 newPostion = new Vector3(player.Position.x, player.Position.y, 0.3f)
            //        + _playerTransform.up * Time.deltaTime * PlayerSpeed;
            //    player.Position = new float2(newPostion.x, newPostion.y);
            //}
            //else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            //{
            //    Vector3 newPostion = new Vector3(player.Position.x, player.Position.y, 0.3f)
            //        - _playerTransform.up * Time.deltaTime * PlayerSpeed;
            //    player.Position = new float2(newPostion.x, newPostion.y);

            //    movingBackwards = true;
            //}

            //// when moving backwards rotation is reverse to make it more natural
            //if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            //    _playerTransform.Rotate(new Vector3(0, 0, movingBackwards ? -PlayerRotationFactor : PlayerRotationFactor));
            //else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            //    _playerTransform.Rotate(new Vector3(0, 0, movingBackwards ? PlayerRotationFactor : -PlayerRotationFactor));
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            _testFlag = true;
            DisposeNativeArrays();
            Application.Quit();
        }
    }

    void DisposeNativeArrays()
    {
        //Positions.Dispose();
        _asteroidArray.Dispose();
    }

    /// <summary>
    /// Updates asteroids' position or respawns them when time comes.
    /// </summary>
    void UpdateAsteroids()
    {
        float deltaTime = Time.deltaTime;

        // asteroids starts at 7
        //for (int i = 7; i < Agents.Length; ++i)
        //{
        //    ref Agent a = ref Agents[i];

        //    if (a.Flags == 2) // asteroid
        //    {
        //        // normal update
        //        a.Position += new float2(
        //            _directionLookupTable[a.DirectionX] * _speedLookupTable[a.Speed],
        //            _directionLookupTable[a.DirectionY] * _speedLookupTable[a.Speed]);

        //        continue;
        //    }
        //    else if (a.Flags == 5) // dead asteroid
        //    {
        //        a.TimeLeftToRespawn -= deltaTime;
        //        if (a.TimeLeftToRespawn <= 0)
        //        {
        //            RespawnAsteroid(ref a);

        //            // insert it back to the collision system
        //            _collisionSystem.Add(0, i);
        //        }
        //    }
        //}
    }

    //void RespawnAsteroid(ref Agent a)
    //{
    //        float2 playerPosition = new float2(0, 0);// Agents[0].Position;

    //    // iterate until you find a position outside of player's frustum
    //    // it is not the most mathematically correct solution
    //    // as the asteroids dispersion will not be even (those that normally would spawn inside the frustum 
    //    // will spawn right next to the frustum's edge instead)
    //    float posX = UnityEngine.Random.Range(0, GridDimensionFloat) + WorldOffetValue;
    //    if (posX > playerPosition.x)
    //    {
    //        // tried to spawn on the right side of the player
    //        if (posX - playerPosition.x < FrustumSizeX)
    //            posX += FrustumSizeX;
    //    }
    //    else
    //    {
    //        // left side
    //        if (playerPosition.x - posX < FrustumSizeX)
    //            posX -= FrustumSizeX;
    //    }

    //    float posY = UnityEngine.Random.Range(0, GridDimensionFloat) + WorldOffetValue;
    //    if (posY > playerPosition.y)
    //    {
    //        // tried to spawn above the player
    //        if (posY - playerPosition.y < FrustumSizeY)
    //            posY += FrustumSizeY;
    //    }
    //    else
    //    {
    //        // below
    //        if (playerPosition.y - posX < FrustumSizeY)
    //            posY -= FrustumSizeY;
    //    }

    //    // respawn
    //    a.Position = new float2(posX, posY);
    //    a.DirectionX = (byte)UnityEngine.Random.Range(0, 256);
    //    a.DirectionY = (byte)UnityEngine.Random.Range(0, 256);
    //    a.Flags = 2; // normal state for a living asteroid
    //}

    /// <summary>
    /// Changes the lasers position and respawn them if necessary.
    /// </summary>
    void UpdateLasers()
    {
        _timeToFireNextLaser -= Time.deltaTime;

        // update laser positions
        for (int i = 0; i < LaserPoolSize; i++)
        {
            //ref Agent laser = ref Agents[i + 1];
            //if (laser.Flags == 1) // laser is alive
            //{
            //    Vector3 newPos = new Vector3(laser.Position.x, laser.Position.y, 0)
            //        + _laserPool[i].transform.up * Time.deltaTime * LaserSpeed;
            //    laser.Position = new float2(newPos.x, newPos.y);
            //}
            //else
            //    laser.Position = new float2(ObjectGraveyardPosition.x, ObjectGraveyardPosition.y);
        }

        // spawn new one every half second
        if (!_playerDestroyed && _timeToFireNextLaser < 0)
        {
            _timeToFireNextLaser = 0.5f;

            //Vector3 spawnPos = new Vector3(Agents[0].Position.x, Agents[0].Position.y, 0f) + _playerTransform.up * 0.4f;

            //// first agent is always player
            //// in the _agents table lasers occupy positions from 1 to 6
            //Agents[_laserNextFreeIndex + 1].Position = new float2(spawnPos.x, spawnPos.y);
            //_laserPool[_laserNextFreeIndex].transform.rotation = _playerTransform.rotation;

            //if (Agents[_laserNextFreeIndex + 1].Flags == 4)
            //{
            //    Agents[_laserNextFreeIndex + 1].Flags = 1;
            //    _collisionSystem.Add(0, _laserNextFreeIndex + 1);
            //}

            //if (++_laserNextFreeIndex > LaserPoolSize - 1)
            //    _laserNextFreeIndex = 0;
        }
    }

    void GameOver()
    {
        _playerDestroyed = true;
        _playerTransform.gameObject.SetActive(false);
        _restartButton.gameObject.SetActive(true);
        _youLoseLabel.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        _playerScore = 0;
        _playerScoreLabel.text = "score: 0";

        _playerTransform.gameObject.SetActive(true);
        _playerTransform.position = new Vector3(
            GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
            GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
            0.3f);
        _playerTransform.rotation = new Quaternion(0, 0, 0, 0);

        //for (int i = 0; i < LaserPoolSize; i++)
        //{
        //    Agents[i + 1].Flags = 4;
        //    Agents[i + 1].Position = new float2(ObjectGraveyardPosition.x, ObjectGraveyardPosition.y);
        //}

        _playerDestroyed = false;
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);

        //InitializeAsteroidsGridLayout();
        //_collisionSystem = new CollisionSystem(
        //    new Quad(
        //        500 + GridDimensionFloat / 2,
        //        1_500 + GridDimensionFloat / 2,
        //        500 + GridDimensionFloat / 2,
        //        1_500 + GridDimensionFloat / 2),
        //    20,
        //    10,
        //    5);

        //_collisionSystem.AddElementsToQuadTree(Agents);
    }

    #region Initializers
    
    void CreateObjectPools()
    {
        for (int i = 0; i < LaserPoolSize; i++)
        {
            _laserPool[i] = Instantiate(_laserBeamPrefab.gameObject);
            //_laserPool[i].transform.position = ObjectGraveyardPosition;
        }
    }
    #endregion
}