using Assets.Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class GameEngine : MonoBehaviour
{
    #region Constants
    public const float AsteroidRadius = 0.2f;
    public const float AsteroidRadius2 = AsteroidRadius + AsteroidRadius;
    const float AsteroidTranformValueZ = 0.4f;
    public const float LaserRadius = 0.07f;
    public const float LaserSpeed = 2.5f;
    public const float LaserFireFrequency = 0.5f;
    public const float LaserLiveLength = 2f;

    const float AsteroidSpeedMin = 0.009f; // distance traveled per frame
    const float AsteroidSpeedMax = 0.02f; // distance traveled per frame
    public const int GridDimensionInt = 200;
    public const float GridDimensionFloat = 200;
    const int TotalNumberOfAsteroids = GridDimensionInt * GridDimensionInt;

    public const float PlayerRadius = 0.2f;
    public const float PlayerRotationFactor = 3f;
    public const float PlayerSpeed = 1.25f;

    const float AsteroidPlayerRadius = AsteroidRadius + PlayerRadius; // to reduce number of additions
    const float AsteroidLaserRadius = AsteroidRadius + LaserRadius; // to reduce number of additions

    public const float WorldOffetValue = 1000f; // we move world top right to be sure we operate on positive numbers

    public const float FrustumSizeX = 6f;
    public const float FrustumSizeY = 4f;
    #endregion

    public static GameEngine Instance { get; private set; }

    public static int NumberOfAsteroidsDestroyedThisFrame;
    public static bool DidPlayerDieThisFrame;

    public Camera SkyboxCamera;

    #region Private Fields

    // prefabs
    // other references
    [SerializeField] Text _playerScoreLabel;
    [SerializeField] Camera _mainCamera;
    [SerializeField] Button _restartButton;
    [SerializeField] Text _youLoseLabel;

    public static int PlayerScore = 0;
    #endregion

    // ECS related
    public EntityManager EntityManager;
    public Mesh QuadMesh;
    public Mesh AsteroidMesh;
    public Mesh SpaceshipMesh;
    public Material AsteroidMaterial;
    public Material SpaceshipMaterial;
    public Material LaserBeamMaterial;
    public GameObject SpaceshipPrefab;
    public static GameObject SpaceshipInstance;

    public const float AsteroidScale = 0.25f;

    // some entities should not be destroyed upon death and just marked instead
    // for example player - camera follows him even if he's dead
    public static EntityArchetype AsteroidArchetype;
    public static EntityArchetype LaserBeamArchetype;
    public static EntityArchetype SpaceshipArchetype;

    // === DEBUG DRAW ===
    [Space]
    [Header("Debug options (slows down the game!)")]
    public bool DrawCollisionQuadrants;
    public bool DrawEntityCollisionBorders;

    void Awake() => Instance = this;

    void Start()
    {
        EntityManager = World.Active.EntityManager;

        CreateArchetypes();
        SpawnAsteroidGrid();
        InitializeSpaceship();

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

        _playerScoreLabel.text = "score: " + PlayerScore;
    }

    void CreateArchetypes()
    {
        AsteroidArchetype = EntityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(MoveSpeedData),
            typeof(Rotation),
            typeof(CollisionTypeData));

        LaserBeamArchetype = EntityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(MoveSpeedData),
            typeof(Rotation),
            typeof(CollisionTypeData),
            typeof(TimeToDieData));

        SpaceshipArchetype = EntityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(LocalToWorld), // how the mesh should be displayed (mandatory in order to be displayed)
            typeof(Translation), // equivalent of position
            typeof(Scale), // uniform scale
            typeof(Rotation),
            typeof(SpaceshipData),
            typeof(CollisionTypeData)
        );
    }

    void SpawnAsteroidGrid()
    {
        for (int x = (int)WorldOffetValue; x < GridDimensionInt + WorldOffetValue; x++)
            for (int y = (int)WorldOffetValue; y < GridDimensionInt + WorldOffetValue; y++)
                SpawnAsteroid(x, y);
    }

    void SpawnAsteroid(float posX, float posY)
    {
        Entity entity = EntityManager.CreateEntity(AsteroidArchetype);
        EntityManager.SetSharedComponentData(
            entity,
            new RenderMesh
            {
                mesh = AsteroidMesh,
                material = AsteroidMaterial
            });

        EntityManager.SetComponentData(entity, new Translation { Value = new float3(posX, posY, 3f) });
        EntityManager.SetComponentData(entity, new Scale { Value = AsteroidScale });

        EntityManager.SetComponentData(
            entity,
            new MoveSpeedData
            {
                DirectionX = UnityEngine.Random.Range(-1f, 1f),
                DirectionY = UnityEngine.Random.Range(-1f, 1f),
                MoveSpeed = UnityEngine.Random.Range(0.05f, 0.2f),
                RotationSpeed = new float3(
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f))
            });

        EntityManager.SetComponentData(entity, new CollisionTypeData { CollisionObjectType = CollisionType.Asteroid });
        EntityManager.SetComponentData(entity, new Rotation { Value = UnityEngine.Random.rotation });
    }

    void InitializeSpaceship()
    {
        SpaceshipInstance = Instantiate(SpaceshipPrefab);
        SpaceshipInstance.transform.position = new Vector3(
            GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
            GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
            3f);
        SpaceshipInstance.transform.Rotate(0, 0, 0);

        Entity spaceship = EntityManager.CreateEntity(SpaceshipArchetype);

        //var color = SpaceshipMaterial.color;
        //color.r += 0.1f;
        //color.g += 0.1f;
        //color.b += 0.1f;

        //SpaceshipMaterial.color = new Color(color.r, color.g, color.b); // I will make it lighter

        EntityManager.SetComponentData(
            spaceship,
            new Translation
            {
                Value = new float3(
                    GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
                    GridDimensionFloat / 2f - 0.5f + WorldOffetValue,
                    3f)
            });

        EntityManager.SetComponentData(spaceship, new Scale { Value = 0.3f });
        EntityManager.SetComponentData(spaceship, new Rotation { Value = SpaceshipInstance.transform.rotation });
        EntityManager.SetComponentData(spaceship, new SpaceshipData { TimeToFireLaser = 0.5f });
        EntityManager.SetComponentData(spaceship, new CollisionTypeData { CollisionObjectType = CollisionType.Player });
    }

    void HandleInput()
    {
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    void GameOver()
    {
        _restartButton.gameObject.SetActive(true);
        _youLoseLabel.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        InitializeSpaceship();

        PlayerScore = 0;
        _playerScoreLabel.text = "score: 0";

        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);
    }
}