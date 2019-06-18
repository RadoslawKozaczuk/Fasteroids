using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class GameEngine : MonoBehaviour
{
    public enum CollisionTypeEnum { Player, Laser, Asteroid }

    #region Constants
    public const float AsteroidRadius = 0.19f;
    public const float AsteroidRadius2 = AsteroidRadius + AsteroidRadius;
    const float AsteroidTranformValueZ = 0.4f;
    const float LaserRadius = 0.07f;
    const float LaserSpeed = 2.5f;

    const float AsteroidSpeedMin = 0.009f; // distance traveled per frame
    const float AsteroidSpeedMax = 0.02f; // distance traveled per frame
    public const int GridDimensionInt = 180;
    public const float GridDimensionFloat = 180;
    const int TotalNumberOfAsteroids = GridDimensionInt * GridDimensionInt;

    const float PlayerRadius = 0.08f;
    public const float PlayerRotationFactor = 3f;
    public const float PlayerSpeed = 1.25f;

    const float AsteroidPlayerRadius = AsteroidRadius + PlayerRadius; // to reduce number of additions
    const float AsteroidLaserRadius = AsteroidRadius + LaserRadius; // to reduce number of additions

    public const float WorldOffetValue = 1000f; // we move world top right to be sure we operate on positive numbers

    // pool sizes
    public const float FrustumSizeX = 3.8f;
    public const float FrustumSizeY = 2.3f;
    #endregion

    public static GameEngine Instance { get; private set; }

    public static int NumberOfAsteroidsDestroyedThisFrame;
    public static bool DidPlayerDieThisFrame;

    #region Private Fields

    // prefabs
    // other references
    [SerializeField] Text _playerScoreLabel;
    [SerializeField] Camera _mainCamera;
    [SerializeField] Button _restartButton;
    [SerializeField] Text _youLoseLabel;

    float _timeToFireNextLaser = 0.5f;
    int _laserNextFreeIndex;
    int _playerScore = 0;
    int _asteroidPoolLastUsedObjectId;
    #endregion

    // ECS related
    public EntityManager EntityManager;
    public Mesh Mesh;
    public Material SpaceshipMaterial;
    public Material LaserBeamMaterial;
    public Material AsteroidMaterial;
    NativeArray<Entity> _spaceshipArray;
    NativeArray<Entity> _laserBeamArray;
    NativeArray<Entity> _asteroidArray;

    // tag component
    public struct SpaceshipData : IComponentData
    {
        public float TimeToFireLaser;
    }

    public struct CollisionTypeData : IComponentData
    {
        public CollisionTypeEnum CollisionObjectType;
    }

    public struct MoveSpeed : IComponentData
    {
        public float DirectionX;
        public float DirectionY;
        public float Speed;
    }

    public struct TimeToRespawn : IComponentData
    {
        public float Time;
    }

    // some entities should not be destroyed upon death and just marked instead
    // for example player - camera follows him even if he's dead
    public struct DeadData : IComponentData { }

    public static EntityArchetype AsteroidArchetype;
    public static EntityArchetype LaserBeamArchetype;

    private void Awake() => Instance = this;

    void Start()
    {
        EntityManager = World.Active.EntityManager;

        AsteroidArchetype = EntityManager.CreateArchetype(
            typeof(RenderMesh), 
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(MoveSpeed),
            typeof(CollisionTypeData));

        LaserBeamArchetype = EntityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(MoveSpeed),
            typeof(CollisionTypeData));

        EntityArchetype spaceshipArchetype = EntityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(Rotation),
            typeof(SpaceshipData),
            typeof(CollisionTypeData)
        );

        _spaceshipArray = new NativeArray<Entity>(1, Allocator.Persistent);
        _asteroidArray = new NativeArray<Entity>(TotalNumberOfAsteroids, Allocator.Persistent);

        EntityManager.CreateEntity(AsteroidArchetype, _asteroidArray); // fill the table with entities

        SpawnAsteroidGrid();

        EntityManager.CreateEntity(spaceshipArchetype, _spaceshipArray); // fill the table with entities

        // initialize spaceship
        Entity spaceship = _spaceshipArray[0];
        EntityManager.SetSharedComponentData(
            spaceship,
            new RenderMesh
            {
                mesh = Mesh,
                material = SpaceshipMaterial
            });

        EntityManager.SetComponentData(
            spaceship,
            new Translation
            {
                Value = new float3(
                    GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
                    GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
                    3f)
            });

        EntityManager.SetComponentData(
            spaceship,
            new Scale { Value = 0.3f });

        EntityManager.SetComponentData(
            spaceship,
            new Rotation { Value = quaternion.RotateZ(0) });

        EntityManager.SetComponentData(
            spaceship,
            new SpaceshipData { TimeToFireLaser = 0.5f });

        EntityManager.SetComponentData(
            spaceship,
            new CollisionTypeData { CollisionObjectType = CollisionTypeEnum.Player });

        _playerScoreLabel.text = "score: 0";
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleInput();

        if (DidPlayerDieThisFrame)
        {
            GameOver();
            DidPlayerDieThisFrame = false;
        }
    }

    void SpawnAsteroidGrid()
    {
        int i = 0;
        for (int x = (int)WorldOffetValue; x < GridDimensionInt + WorldOffetValue; x++)
            for (int y = (int)WorldOffetValue; y < GridDimensionInt + WorldOffetValue; y++)
            {
                Entity entity = _asteroidArray[i++];
                EntityManager.SetSharedComponentData(
                    entity,
                    new RenderMesh
                    {
                        mesh = Mesh,
                        material = AsteroidMaterial
                    });

                EntityManager.SetComponentData(
                    entity,
                    new Translation { Value = new float3(x, y, 3f) });

                EntityManager.SetComponentData(
                    entity,
                    new Scale { Value = 0.6f });

                EntityManager.SetComponentData(
                    entity,
                    new MoveSpeed
                    {
                        DirectionX = UnityEngine.Random.Range(-1f, 1f),
                        DirectionY = UnityEngine.Random.Range(-1f, 1f),
                        Speed = UnityEngine.Random.Range(0.05f, 0.2f)
                    });

                EntityManager.SetComponentData(
                    entity,
                    new CollisionTypeData { CollisionObjectType = CollisionTypeEnum.Asteroid });
            }
    }

    void HandleInput()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            DisposeNativeArrays();
            Application.Quit();
        }
    }

    void DisposeNativeArrays()
    {
        _asteroidArray.Dispose();
    }

    void GameOver()
    {
        _restartButton.gameObject.SetActive(true);
        _youLoseLabel.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        _playerScore = 0;
        _playerScoreLabel.text = "score: 0";

        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);
    }
}