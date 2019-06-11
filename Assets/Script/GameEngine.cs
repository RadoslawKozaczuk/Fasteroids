using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class GameEngine : MonoBehaviour
{
    [StructLayout(LayoutKind.Explicit)]
    struct FloatIntUnion
    {
        [FieldOffset(0)]
        public float f;

        [FieldOffset(0)]
        public int tmp;
    }

    #region Constants
    const float AsteroidRadius = 0.20f;
    const float AsteroidRadius2 = AsteroidRadius + AsteroidRadius;
    const float AsteroidTranformValueZ = 0.4f;
    const float LaserRadius = 0.08f;
    const float LaserSpeed = 2.5f;

    const float AsteroidSpeedMin = 0.010f; // distance traveled per frame
    const float AsteroidSpeedMax = 0.025f; // distance traveled per frame
    const int GridDimensionInt = 160;
    const float GridDimensionFloat = 160;
    const int TotalNumberOfAsteroids = GridDimensionInt * GridDimensionInt;

    const float PlayerRadius = 0.08f;
    const float PlayerRotationFactor = 3f;
    const float PlayerSpeed = 1.25f;

    const float AsteroidPlayerRadius = AsteroidRadius + PlayerRadius; // to reduce number of additions
    const float AsteroidLaserRadius = AsteroidRadius + LaserRadius; // to reduce number of additions

    // pool sizes
    const int AsteroidPoolSize = 40; // in tests this never went above 35 so for safety i gave 5 more
    const int LaserPoolSize = 6; // we shot once per 0.5 frame and the lifetime is 3s

    const float FrustumSizeX = 3.8f;
    const float FrustumSizeY = 2.3f;
    #endregion

    #region Private Fields
    // readonly fields and tables
    static readonly float[] _speedLookupTable = new float[256];
    static readonly float[] _directionLookupTable = new float[256];
    static readonly Asteroid[] _asteroids = new Asteroid[TotalNumberOfAsteroids];

    // this is were unused object goes upon death
    static readonly Vector3 _objectGraveyardPosition = new Vector3(-99999, -99999, 0.3f);

    // prefabs
    [SerializeField] GameObject _asteroidPrefab;
    [SerializeField] GameObject _laserBeamPrefab;
    [SerializeField] GameObject _spaceshipPrefab;

    // other references
    [SerializeField] Text _playerScoreLabel;
    [SerializeField] Camera _mainCamera;
    [SerializeField] Button _restartButton;
    [SerializeField] Text _youLoseLabel;

    // object pools
    GameObject[] _asteroidPool;
    GameObject[] _laserPool;

    // cached position tables - to reduce number of transform accessors calls
    Vector3[] _laserCachedPositions;
    Vector3 _playerCachedPosition;

    Transform _playerTransform;
    bool[] _laserDestructionFlags; // indicates if the corresponding laser beam is destroyed
    float _timeToFireNextLaser = 0.5f;
    int _laserNextFreeIndex;
    int _playerScore = 0;
    bool _playerDestroyed;
    #endregion

    void Start()
    {
        _playerScoreLabel.text = "score: 0";
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);

        CreateObjectPoolsAndTables();
        InitializeLookupTables(AsteroidSpeedMin, AsteroidSpeedMax);
        InitializeAsteroidsGridLayout();
        SortAsteroids(0, TotalNumberOfAsteroids - 1, 2 * FloorLog2(TotalNumberOfAsteroids));

        _playerTransform = Instantiate(_spaceshipPrefab).transform;
        _playerTransform.position = new Vector3(
            GridDimensionFloat / 2f - 0.5f,
            GridDimensionFloat / 2f - 0.5f,
            0.3f);
    }

    void Update()
    {
        // Transform.position is an accessor and calling it results in a calculation behind the scenes
        // so we cache it for the time of the frame calculation
        _playerCachedPosition = _playerTransform.position;
        for (int i = 0; i < LaserPoolSize; i++)
            _laserCachedPositions[i] = _laserPool[i].transform.position;

        // we do everything in one class to reduce the number of Update calls
        // small gain but maximum speed means maximum speed

        HandleInput();

        UpdateAsteroids();
        SortAsteroids(0, TotalNumberOfAsteroids - 1, 2 * FloorLog2(TotalNumberOfAsteroids));
        UpdateLasers();

        CheckCollisionsBetweenAsteroids();
        CheckCollisionsWithShipAndBullets();
        ShowVisibleAsteroids();

        // after calculation is done we can assign it back to the transform
        _playerTransform.position = _playerCachedPosition;
        for (int i = 0; i < LaserPoolSize; i++)
            _laserPool[i].transform.position = _laserCachedPositions[i];

        if (!_playerDestroyed)
            _mainCamera.transform.position = new Vector3(_playerCachedPosition.x, _playerCachedPosition.y, 0f);
    }

    #region Dedicated Sorting Methods
    /*  All the code in this region is just decompiled and slightly optimized System.Array.Sort() method.
 
        To gain some speed boost I deleted:
            - all the safety checks as we know what data are we going to deal with anyway 
                almost insignificant in terms of speed gain but always something
            - all the boiler plate code, interfaces, generics etc.
            - changed the code so the reference to the table is no longer passed from a method to a method - tiny gain
            - and most importantly in some cases I could get rid of passing items by value
                and instead we just pass its index in the table - the reduces greatly the number of the struct copying
     
        All in all improvements above gave me 0.5 ms shorter sorting time (measured in the editor). 
    */

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int FloorLog2(int n)
    {
        int num = 0;
        for (; n >= 1; n /= 2)
            ++num;
        return num;
    }

    void SwapIfGreater(int a, int b)
    {
        if (a == b || Compare(a, b) <= 0)
            return;

        Asteroid temp = _asteroids[a];
        _asteroids[a] = _asteroids[b];
        _asteroids[b] = temp;
    }

    void Swap(int i, int j)
    {
        if (i == j)
            return;
        Asteroid temp = _asteroids[i];
        _asteroids[i] = _asteroids[j];
        _asteroids[j] = temp;
    }

    void SortAsteroids(int lo, int hi, int depthLimit)
    {
        int num1;
        for (; hi > lo; hi = num1 - 1)
        {
            int num2 = hi - lo + 1;
            if (num2 <= 16)
            {
                if (num2 == 1)
                    break;
                if (num2 == 2)
                {
                    SwapIfGreater(lo, hi);
                    break;
                }
                if (num2 == 3)
                {
                    SwapIfGreater(lo, hi - 1);
                    SwapIfGreater(lo, hi);
                    SwapIfGreater(hi - 1, hi);
                    break;
                }
                InsertionSort(lo, hi);
                break;
            }
            if (depthLimit == 0)
            {
                Heapsort(lo, hi);
                break;
            }
            --depthLimit;
            num1 = PickPivotAndPartition(lo, hi);
            SortAsteroids(num1 + 1, hi, depthLimit);
        }
    }

    int PickPivotAndPartition(int lo, int hi)
    {
        int index = lo + (hi - lo) / 2;
        SwapIfGreater(lo, index);
        SwapIfGreater(lo, hi);
        SwapIfGreater(index, hi);
        Asteroid key = _asteroids[index];
        Swap(index, hi - 1);
        int i = lo;
        int j = hi - 1;
        while (i < j)
        {
            do
                ;
            while (Compare(++i, key) < 0);
            do
                ;
            while (Compare(key, --j) < 0);
            if (i < j)
                Swap(i, j);
            else
                break;
        }
        Swap(i, hi - 1);
        return i;
    }

    void Heapsort(int lo, int hi)
    {
        int n = hi - lo + 1;
        for (int i = n / 2; i >= 1; --i)
            DownHeap(i, n, lo);
        for (int index = n; index > 1; --index)
        {
            Swap(lo, lo + index - 1);
            DownHeap(1, index - 1, lo);
        }
    }

    void DownHeap(int i, int n, int lo)
    {
        Asteroid key = _asteroids[lo + i - 1];
        int num;
        for (; i <= n / 2; i = num)
        {
            num = 2 * i;
            if (num < n && Compare(lo + num - 1, lo + num) < 0)
                ++num;
            if (Compare(key, lo + num - 1) < 0)
                _asteroids[lo + i - 1] = _asteroids[lo + num - 1];
            else
                break;
        }
        _asteroids[lo + i - 1] = key;
    }

    void InsertionSort(int lo, int hi)
    {
        for (int index1 = lo; index1 < hi; ++index1)
        {
            int index2 = index1;
            Asteroid key;
            for (key = _asteroids[index1 + 1]; index2 >= lo && Compare(key, index2) < 0; --index2)
                _asteroids[index2 + 1] = _asteroids[index2];
            _asteroids[index2 + 1] = key;
        }
    }

    // a > b = 1
    // a == b = 0
    // a < b = -1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int Compare(int a, int b) => _asteroids[a].Position.x >= _asteroids[b].Position.x ? 1 : -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int Compare(Asteroid a, int b) => a.Position.x >= _asteroids[b].Position.x ? 1 : -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int Compare(int a, Asteroid b) => _asteroids[a].Position.x >= b.Position.x ? 1 : -1;
    #endregion

    void HandleInput()
    {
        if (!_playerDestroyed)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                _playerCachedPosition += _playerTransform.up * Time.deltaTime * PlayerSpeed;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                _playerCachedPosition -= _playerTransform.up * Time.deltaTime * PlayerSpeed;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                _playerTransform.Rotate(new Vector3(0, 0, PlayerRotationFactor));
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                _playerTransform.Rotate(new Vector3(0, 0, -PlayerRotationFactor));
        }

        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    /// <summary>
    /// Updates asteroids' position or respawns them when time comes.
    /// </summary>
    void UpdateAsteroids()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < TotalNumberOfAsteroids; ++i)
        {
            ref Asteroid a = ref _asteroids[i];

            if (a.Flags == 0)
            {
                // normal update
                a.Position += new float2(
                    _directionLookupTable[a.DirectionX] * _speedLookupTable[a.Speed],
                    _directionLookupTable[a.DirectionY] * _speedLookupTable[a.Speed]);

                continue;
            }

            a.TimeLeftToRespawn -= deltaTime;
            if (a.TimeLeftToRespawn <= 0)
                RespawnAsteroid(ref a);
        }
    }

    void RespawnAsteroid(ref Asteroid a)
    {
        // iterate until you find a position outside of player's frustum
        // it is not the most mathematically correct solution
        // as the asteroids dispersion will not be even (those that normally would spawn inside the frustum 
        // will spawn right next to the frustum's edge instead)
        float posX = UnityEngine.Random.Range(0, GridDimensionFloat);
        if (posX > _playerCachedPosition.x)
        {
            // tried to spawn on the right side of the player
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerCachedPosition.x;
            if (value2 < 0)
                value2 *= -1;

            if (value1 - value2 < FrustumSizeX)
                posX += FrustumSizeX;
        }
        else
        {
            // left side
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerCachedPosition.x;
            if (value2 < 0)
                value2 *= -1;

            if (value2 - value1 < FrustumSizeX)
                posX -= FrustumSizeX;
        }

        float posY = UnityEngine.Random.Range(0, GridDimensionFloat);
        if (posY > _playerCachedPosition.y)
        {
            // tried to spawn above the player
            float value1 = posY;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerCachedPosition.y;
            if (value2 < 0)
                value2 *= -1;

            if (value1 - value2 < FrustumSizeY)
                posY += FrustumSizeY;
        }
        else
        {
            // below
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerCachedPosition.y;
            if (value2 < 0)
                value2 *= -1;

            if (value2 - value1 < FrustumSizeY)
                posY -= FrustumSizeY;
        }

        // respawn
        a.Position = new float2(posX, posY);
        a.DirectionX = (byte)UnityEngine.Random.Range(0, 256);
        a.DirectionY = (byte)UnityEngine.Random.Range(0, 256);
        a.Flags = 0; // normal state for a living asteroid
    }

    /// <summary>
    /// Changes the lasers position and respawn them if necessary.
    /// </summary>
    void UpdateLasers()
    {
        _timeToFireNextLaser -= Time.deltaTime;

        // update laser positions
        for (int i = 0; i < LaserPoolSize; i++)
            if (!_laserDestructionFlags[i])
                _laserCachedPositions[i] += _laserPool[i].transform.up * Time.deltaTime * LaserSpeed;

        // spawn new one every half second
        if (!_playerDestroyed && _timeToFireNextLaser < 0)
        {
            _timeToFireNextLaser = 0.5f;
            _laserDestructionFlags[_laserNextFreeIndex] = false;
            _laserCachedPositions[_laserNextFreeIndex] = _playerCachedPosition + _playerTransform.up * 0.3f;
            _laserPool[_laserNextFreeIndex].transform.rotation = _playerTransform.rotation;

            if (++_laserNextFreeIndex > LaserPoolSize - 1)
                _laserNextFreeIndex = 0;
        }
    }

    /// <summary>
    /// Check if there is any collision between any two asteroids in the game.
    /// Updates the game state if any collision has been found.
    /// </summary>
    void CheckCollisionsBetweenAsteroids()
    {
        // the last one is the last to the right it does not need to be processed because
        // its collisions are already handled by the ones preceding him
        for (int indexA = 0; indexA < TotalNumberOfAsteroids - 1; indexA++)
        {
            int indexB = indexA + 1;
            ref Asteroid a = ref _asteroids[indexA];
            ref Asteroid b = ref _asteroids[indexB];

            float difX = b.Position.x - a.Position.x;
            if (difX >= AsteroidRadius2)
                continue; // b is too far on x axis

            // a is destroyed
            if (a.Flags == 1)
                continue;

            // check for other asteroids
            while (indexB < TotalNumberOfAsteroids - 1)
            {
                float difY = b.Position.y - a.Position.y;
                if (difY < 0)
                    difY *= -1;

                if (difY >= AsteroidRadius2)
                {
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius2)
                        break; // b is too far on x axis
                    continue;
                }

                // b is destroyed
                if (b.Flags == 1)
                {
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius2)
                        break; // b is too far on x axis
                    continue;
                }

                // FastSqrt offers better performance for slightly less accurate results
                // additionally we perform manual power^2 instead of call to the function 
                // provided because again it is faster this way
                float distance = FastSqrt(difX * difX + difY * difY);
                if (distance < AsteroidRadius2)
                {
                    // collision! mark both as destroyed in this frame and break the loop
                    a.Flags = 1; // destroyed
                    b.Flags = 1; // destroyed
                    a.TimeLeftToRespawn = 1f;
                    b.TimeLeftToRespawn = 1f;
                    ++indexA; // increase by one here and again in the for loop
                    break;
                }
                else
                {
                    // no collision with this one but it maybe with the next one
                    // as long as the x difference is lower than Radius * 2
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius2)
                        break; // b is too far on x axis
                }
            };
        }
    }

    /// <summary>
    /// Check if there is any collision between any asteroid and any laser or player.
    /// Updates the game state if any collision has been found.
    /// </summary>
    void CheckCollisionsWithShipAndBullets()
    {
        float lowestX = _playerCachedPosition.x;
        float highestX = _playerCachedPosition.x;

        for (int i = 0; i < LaserPoolSize; i++)
        {
            if (_laserCachedPositions[i].x < lowestX)
                lowestX = _laserCachedPositions[i].x;
            else if (_laserCachedPositions[i].x > highestX)
                highestX = _laserCachedPositions[i].x;
        }

        // find the range within collision is possible
        for (int i = 0; i < TotalNumberOfAsteroids; i++)
        {
            ref Asteroid a = ref _asteroids[i];

            // omit destroyed
            if (a.Flags == 1)
                continue;

            if (a.Position.x < lowestX)
            {
                float value = lowestX - a.Position.x;
                if (value < 0)
                    value *= -1;

                // here we need to be careful to always pick the radius that is higher
                // for now both player and laser have the same radius
                if (value > AsteroidPlayerRadius)
                    continue; // no collisions possible
            }
            else if (a.Position.x > highestX)
            {
                float value = highestX - a.Position.x;
                if (value < 0)
                    value *= -1;

                // same here my friend - if the laser's would become bigger pick the AsteroidLaserRadius constant instead
                if (value > AsteroidPlayerRadius)
                    break; // no collisions possible neither for this nor for all the rest
            }

            // check asteroid collision with lasers first
            float distance;
            for (int j = 0; j < LaserPoolSize; j++)
            {
                if (_laserDestructionFlags[j])
                    continue;

                // calculate the distance between the asteroid and the laser
                distance = FastSqrt(
                    (_laserCachedPositions[j].x - a.Position.x) * (_laserCachedPositions[j].x - a.Position.x)
                    + (_laserCachedPositions[j].y - a.Position.y) * (_laserCachedPositions[j].y - a.Position.y));

                // collision
                if (distance < AsteroidLaserRadius)
                {
                    UpdateScore();
                    _laserDestructionFlags[j] = true;
                    _laserCachedPositions[j] = _objectGraveyardPosition;
                    a.Flags = 1; // destroyed
                    a.TimeLeftToRespawn = 1f;
                    break;
                }
            }

            // asteroid destroyed in the loop above
            if (a.Flags == 1)
                continue;

            if (_playerDestroyed)
                continue;

            // check collision with the player
            distance = FastSqrt(
                (_playerCachedPosition.x - a.Position.x) * (_playerCachedPosition.x - a.Position.x)
                + (_playerCachedPosition.y - a.Position.y) * (_playerCachedPosition.y - a.Position.y));

            if (distance < AsteroidPlayerRadius)
            {
                // this asteroid destroyed player
                a.Flags = 1;
                GameOverFunction();
            }
        }
    }

    void GameOverFunction()
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
            GridDimensionFloat / 2f - 0.5f,
            GridDimensionFloat / 2f - 0.5f,
            0.3f);
        _playerTransform.rotation = new Quaternion(0, 0, 0, 0);

        for (int i = 0; i < LaserPoolSize; i++)
        {
            _laserDestructionFlags[i] = true;
            _laserCachedPositions[i] = _objectGraveyardPosition;
        }

        _playerDestroyed = false;
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);

        InitializeAsteroidsGridLayout();
    }

    void ShowVisibleAsteroids()
    {
        int poolElementIndex = 0;

        for (int i = 0; i < TotalNumberOfAsteroids; i++)
        {
            ref Asteroid a = ref _asteroids[i];

            if (a.Flags == 1)
                continue;

            // is visible in x?
            float value = _playerCachedPosition.x - a.Position.x;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeX)
                continue;

            // is visible in y?
            value = _playerCachedPosition.y - a.Position.y;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeY)
                continue;

            // take first from the pool
            _asteroidPool[poolElementIndex++].gameObject.transform.position = new Vector3(
                a.Position.x,
                a.Position.y,
                AsteroidTranformValueZ);
        }

        // unused objects go to the graveyard
        while (poolElementIndex < AsteroidPoolSize)
            _asteroidPool[poolElementIndex++].transform.position = _objectGraveyardPosition;
    }

    void UpdateScore() => _playerScoreLabel.text = "score: " + ++_playerScore;

    #region Initializers
    void InitializeAsteroidsRandomPosition()
    {
        for (int x = 0, i = 0; x < GridDimensionInt; x++)
            for (int y = 0; y < GridDimensionInt; y++)
            {
                _asteroids[i++] = new Asteroid()
                {
                    Position = new float2(
                        UnityEngine.Random.Range(0, GridDimensionFloat),
                        UnityEngine.Random.Range(0, GridDimensionFloat)),
                    DirectionX = (byte)UnityEngine.Random.Range(0, 256),
                    DirectionY = (byte)UnityEngine.Random.Range(0, 256),
                    Speed = (byte)UnityEngine.Random.Range(0, 256),
                    Flags = 0
                };
            }
    }

    void InitializeAsteroidsGridLayout()
    {
        for (int x = 0, i = 0; x < GridDimensionInt; x++)
            for (int y = 0; y < GridDimensionInt; y++)
                _asteroids[i++] = new Asteroid()
                {
                    Position = new float2(x, y),
                    DirectionX = (byte)UnityEngine.Random.Range(0, 256),
                    DirectionY = (byte)UnityEngine.Random.Range(0, 256),
                    Speed = (byte)UnityEngine.Random.Range(0, 256),
                    Flags = 0
                };
    }

    void CreateObjectPoolsAndTables()
    {
        _asteroidPool = new GameObject[AsteroidPoolSize];
        for (int i = 0; i < AsteroidPoolSize; i++)
        {
            _asteroidPool[i] = Instantiate(_asteroidPrefab.gameObject);
            _asteroidPool[i].transform.position = _objectGraveyardPosition;
        }

        _laserPool = new GameObject[LaserPoolSize];
        _laserCachedPositions = new Vector3[LaserPoolSize];
        _laserDestructionFlags = new bool[LaserPoolSize];
        for (int i = 0; i < LaserPoolSize; i++)
        {
            _laserPool[i] = Instantiate(_laserBeamPrefab.gameObject);
            _laserPool[i].transform.position = _objectGraveyardPosition;
            _laserCachedPositions[i] = _objectGraveyardPosition;
        }
    }

    void InitializeLookupTables(float speedMin, float speedMax)
    {
        for (int i = 0; i < 256; i++)
        {
            if (i == 0)
                _speedLookupTable[0] = speedMin;
            else
                _speedLookupTable[i] = speedMin + i / 255f * (speedMax - speedMin);
        }

        for (int i = 0; i < 256; i++)
            _directionLookupTable[i] = i < 127
                ? -1f * (128 - i) / 128 // it goes from - 1f to 0
                : i == 127 || i == 128 // then we have two 0s for symmetry
                    ? 0
                    : (i - 127) / 128f; // and from 0 to 1f
    }
    #endregion

    // not written by me, I found it on the Internet
    // it is around 10 - 15% faster than the Mathf.Sqrt from Unity.Mathematics 
    // (which probably uses the inverse square root method from Quake 3 based on its cost).
    // but that comes for a cost of less accurate approximation (from 0.5% to 5% less accurate)
    float FastSqrt(float number)
    {
        if (number == 0)
            return 0;

        FloatIntUnion u;
        u.tmp = 0;
        u.f = number;
        u.tmp -= 1 << 23; /* Subtract 2^m. */
        u.tmp >>= 1; /* Divide by 2. */
        u.tmp += 1 << 29; /* Add ((b + 1) / 2) * 2^m. */
        return u.f;
    }

    /// <summary>
    /// Simple test method to test if the array is sorted properly.
    /// Useful for potentially dangerous sorting optimizations.
    /// </summary>
    bool SortAlgorithmValidityCheck()
    {
        for (int i = 0; i < TotalNumberOfAsteroids - 1; i++)
            if (_asteroids[i].Position.x > _asteroids[i + 1].Position.x)
            {
                Debug.LogError("The sorting algorithm is invalid");
                return false;
            }

        return true;
    }
}
