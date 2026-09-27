using System.Collections;
using Procedural_Generation;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Enemies.Common;
using Players;

public class LevelGenerator : MonoBehaviour {
    [SerializeField] private GameObject armadilloPrefab;
    [SerializeField] private GameObject crabPrefab;
    [SerializeField] private GameObject birdPrefab;
    [SerializeField] private GameObject sundewPrefab;

    public Tilemap masterTilemap; // The main Tilemap
    [SerializeField] private GameObject startPrefab; // The starting piece. It should have exactly one connector.
    [SerializeField] private GameObject endPieceUp;
    [SerializeField] private GameObject endPieceDown;
    [SerializeField] private GameObject endPieceLeft;
    [SerializeField] private GameObject endPieceRight;
    [SerializeField] private GameObject fillerPrefab;

    public GameObject[] sectionPrefabs; // Prefabs containing tilemap sections (tunnels, rooms, etc.)

    [SerializeField] private GameObject entryDoorPrefab;
    [SerializeField] private GameObject exitDoorPrefab;

    public Vector3Int startingPosition; // Initial position to start generation

    private List<Connector> openConnectors = new List<Connector>(); // List of currently open connectors
    private List<Vector3> groundSpawnPoints = new List<Vector3>();
    private List<Vector3> airSpawnPoints = new List<Vector3>();
    private HashSet<Vector2Int> occupiedSpaces = new HashSet<Vector2Int>();

    private Vector3 endPosition;

    [SerializeField] internal int pieceWidth = 24;
    [SerializeField] internal int pieceHeight = 12;

    Vector2Int WorldToPieceGrid(Vector3 position) {
        return new Vector2Int(Mathf.RoundToInt(position.x / (pieceWidth * 2)), Mathf.RoundToInt(position.y / (pieceHeight * 2)));
    }


    void Start() {
        GenerateLevel();
    }

    void GenerateLevel() {
        Vector3 position = startingPosition;

        // Spawn the entry door
        Instantiate(entryDoorPrefab, startingPosition + new Vector3Int(0, 1, 0), Quaternion.identity);

        // Spawn the first section
        GameObject firstPrefab = Instantiate(startPrefab, position, Quaternion.identity);
        CopyTilesToMasterTilemap(firstPrefab.GetComponent<Tilemap>(), Vector3Int.zero);
        occupiedSpaces.Add(WorldToPieceGrid(Vector2.zero));


        Connector[] firstConnectors = FindConnectors(firstPrefab);

        // Add its connectors to the open list
        foreach (Connector conn in firstConnectors) {
            openConnectors.Add(conn);
        }

        for (int i = 1; i < 10; i++) {
            if (openConnectors.Count == 0) {
                Debug.LogWarning("No open connectors left. Stopping generation.");
                break;
            }

            // Choose a random open connector to build from
            Connector chosenConnector = openConnectors[Random.Range(0, openConnectors.Count)];
            Vector3 chosenWorldPosition = chosenConnector.transform.position;

// Find a valid prefab that has a compatible connector type
            GameObject prefabToUse = GetCompatiblePrefab(chosenConnector.connectorType, chosenWorldPosition);

            if (prefabToUse == null) {
                Debug.LogWarning("No valid prefab found for connector type: " + chosenConnector.connectorType);
                openConnectors.Remove(chosenConnector);
                continue;
            }

// Instantiate the new prefab
            GameObject instantiatedPrefab = Instantiate(prefabToUse, chosenWorldPosition, Quaternion.identity);

// Find the matching connector in the instantiated prefab
            Connector matchingConnector = null;
            foreach (Connector conn in FindConnectors(instantiatedPrefab)) {
                if (conn.connectorType == Connector.GetCompatibleConnectorType(chosenConnector.connectorType)) {
                    matchingConnector = conn;
                    break;
                }
            }

            if (matchingConnector == null) {
                Debug.LogError("Failed to find matching connector in instantiated prefab");
                Destroy(instantiatedPrefab);
                openConnectors.Remove(chosenConnector);
                continue;
            }

// Calculate the offset to align the connectors
            Vector3 offset = chosenWorldPosition - matchingConnector.transform.position;
            instantiatedPrefab.transform.position += 3 * offset;

            Tilemap secondTilemap = instantiatedPrefab.GetComponent<Tilemap>();

            CopyTilesToMasterTilemap(secondTilemap, Vector3Int.RoundToInt(instantiatedPrefab.transform.position));

            occupiedSpaces.Add(WorldToPieceGrid(instantiatedPrefab.transform.position));

            openConnectors.Remove(chosenConnector);

// Only add the other connector from the prefab (since each prefab has exactly 2)
            Connector[] newConnectors = FindConnectors(instantiatedPrefab);
            foreach (Connector conn in newConnectors) {
                if (conn != matchingConnector) {
                    openConnectors.Add(conn);
                    break; // Only add one connector (the non-matching one)
                }
            }

            EnemySpawnPoint[] foundPoints = FindSpawnPoints(instantiatedPrefab);

            foreach (EnemySpawnPoint spawnPoint in foundPoints) {
                if (spawnPoint.type == SpawnPointType.Ground) {
                    groundSpawnPoints.Add(spawnPoint.transform.position + instantiatedPrefab.transform.position);
                } else if (spawnPoint.type == SpawnPointType.Air) {
                    airSpawnPoints.Add(spawnPoint.transform.position + instantiatedPrefab.transform.position);
                }
            }
        }

        // generate the end section
        if (openConnectors.Count > 0) {
            Connector lastConnector = openConnectors[0]; // The last remaining open connector
            Vector3 lastPosition = lastConnector.transform.position;

            // Choose the appropriate end piece based on connector type
            GameObject endPiecePrefab = GetEndPiecePrefab(lastConnector.connectorType);

            if (endPiecePrefab != null) {
                // Instantiate and align the end piece
                GameObject endPiece = Instantiate(endPiecePrefab, lastPosition, Quaternion.identity);

                // Find its single connector (to align it properly)
                Connector endConnector = FindConnectors(endPiece).FirstOrDefault();
                if (endConnector != null) {
                    Vector3 offset = lastPosition - endConnector.transform.position;
                    endPiece.transform.position += offset * 3;
                }

                // Copy tiles from the end piece to the master tilemap
                CopyTilesToMasterTilemap(endPiece.GetComponent<Tilemap>(), Vector3Int.RoundToInt(endPiece.transform.position));

                endPosition = endPiece.transform.position / 2;

                // Mark space as occupied
                occupiedSpaces.Add(WorldToPieceGrid(endPiece.transform.position));
            }

            Instantiate(exitDoorPrefab, endPosition += new Vector3(0, 2, 0), Quaternion.identity);
        }

        // Wrap the generated level with filler pieces on all sides.
        var neighbours = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up + Vector2Int.left, Vector2Int.up + Vector2Int.right, Vector2Int.down + Vector2Int.left, Vector2Int.down + Vector2Int.right };
        foreach (var instance in occupiedSpaces.SelectMany(space =>
                     from offset in neighbours select space + offset
                     into neighbour
                     where !occupiedSpaces.Contains(neighbour) select Instantiate(fillerPrefab, new Vector3(neighbour.x * pieceWidth * 2, neighbour.y * pieceHeight * 2, 0), Quaternion.identity))) {
            CopyTilesToMasterTilemap(instance.GetComponent<Tilemap>(), Vector3Int.RoundToInt(instance.transform.position));
        }

        float enemyDifficulty = CalculateEnemyDifficulty(PlayerDataManager.Instance.warpedDepthsProgress);

        float enemyMagnification = CalculateEnemyMagnification(PlayerDataManager.Instance.warpedDepthsProgress);

        StartCoroutine(SpawnEnemiesWithAStar(10, startingPosition, endPosition, enemyDifficulty, enemyMagnification));
        SpawnEnemiesAtPoints(enemyDifficulty, enemyMagnification);
    }

    // returns a float between 0.3 and 1 representing the chance for any enemy spawn to actually happen.
    // Scales linearly with progress, and reaches 1 past level 10.
    private static float CalculateEnemyDifficulty(int progress) {
        return Mathf.Clamp01(Mathf.Lerp(0.3f, 1f, (float)progress / 10));
    }


    // returns a float >=1 that represents the factor by which to multiply all enemy stats.
    // Example: factor = 2 -> enemies have double health and damage.
    // Enemy stats will grow exponentially, by doubling every five levels.
    private static float CalculateEnemyMagnification(int progress) {
        return Mathf.Pow(2f, progress / 5f);
    }


    private void SpawnEnemiesAtPoints(float probability, float magnification) {
        foreach (Vector3 spawnPoint in groundSpawnPoints) {
            if (Random.value > probability) continue;
            // i dont know why the shit is scaled by 4 LOL
            SpawnGroundEnemy(spawnPoint / 4, magnification);
        }

        foreach (Vector3 spawnPoint in airSpawnPoints) {
            if (Random.value > probability) continue;
            SpawnAirEnemy(spawnPoint / 4, magnification);
        }
    }


    public IEnumerator SpawnEnemiesWithAStar(int enemyCount, Vector3 startPoint, Vector3 endPoint, float probability, float magnification) {
        yield return new WaitForEndOfFrame();
        Pathfinder.Instance.UpdateTilemap();
        List<Vector3> path = Pathfinder.FindPath(startPoint, endPoint);
        if (path.Count == 0) {
            Debug.LogWarning("No valid path found for enemy spawning!");
        }

        for (int i = 0; i < path.Count - 1; i++) {
            Debug.DrawLine(path[i], path[i + 1], Color.green, 99f);
        }

        for (int i = 0; i < enemyCount; i++) {
            int index = Mathf.FloorToInt((float)i / enemyCount * (path.Count - 1));

            if (Vector3.Distance(path[index], Vector3.zero) < 3f || Vector3.Distance(path[index], endPoint) < 3f) continue;
            // skip if random chance doesn't cooperate. we don't spawn unlucky enemies here
            if (Random.value > probability) continue;
            SpawnRandomEnemy(path[index], magnification);
        }
    }

    private void SpawnRandomEnemy(Vector3 location, float magnification) {
        if (Random.value > 0.33f) {
            SpawnGroundEnemy(location, magnification);
        } else {
            SpawnAirEnemy(location, magnification);
        }
    }

    private void SpawnGroundEnemy(Vector3 location, float magnification) {
        GameObject instance;
        if (Random.value < .5f) {
             instance = Instantiate(armadilloPrefab, location, Quaternion.identity);

        } else {
            instance = Instantiate(crabPrefab, location, Quaternion.identity);
        }

        Enemy enemy = instance.GetComponent<Enemy>();
        enemy.Magnify(magnification);
    }

    private void SpawnAirEnemy(Vector3 location, float magnification) {
        GameObject instance = Instantiate(birdPrefab, location, Quaternion.identity);

        Enemy enemy = instance.GetComponent<Enemy>();
        enemy.Magnify(magnification);
    }

    // Find all connectors in a prefab
    Connector[] FindConnectors(GameObject prefab) {
        return prefab.GetComponentsInChildren<Connector>();
    }


    EnemySpawnPoint[] FindSpawnPoints(GameObject prefab) {
        return prefab.GetComponentsInChildren<EnemySpawnPoint>();
    }


    // Find a valid prefab that has a connector matching the required type
    GameObject GetCompatiblePrefab(ConnectorType requiredType, Vector3 position) {
        GameObject[] shuffledPrefabs = (GameObject[])sectionPrefabs.Clone();
        ShufflePrefabs(shuffledPrefabs);

        foreach (GameObject prefab in shuffledPrefabs) {
            Connector[] connectors = FindConnectors(prefab);
            ShuffleConnectors(connectors);

            Connector matchingConnector = null;
            Connector exitConnector = null;

            foreach (Connector conn in connectors) {
                if (conn.connectorType == Connector.GetCompatibleConnectorType(requiredType))
                    matchingConnector = conn;
                else
                    exitConnector = conn;
            }

            if (matchingConnector == null || exitConnector == null)
                continue;

            // Calculate exit piece space
            Vector3 exitWorldPosition = position + (exitConnector.transform.position - matchingConnector.transform.position) * 3;

            if (IsPieceSpaceAvailable(exitWorldPosition)) {
                return prefab;
            }
        }

        return null;
    }


    void ShufflePrefabs(GameObject[] prefabs) {
        for (int i = prefabs.Length - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            (prefabs[i], prefabs[randomIndex]) = (prefabs[randomIndex], prefabs[i]);
        }
    }


    // Copy tiles to the master tilemap
    void CopyTilesToMasterTilemap(Tilemap tilemap, Vector3Int positionOffset) {
        foreach (var pos in tilemap.cellBounds.allPositionsWithin) {
            TileBase tile = tilemap.GetTile(pos);
            if (tile != null) {
                masterTilemap.SetTile(positionOffset + pos, tile);
            }
        }

        Destroy(tilemap.gameObject);
    }

    void ShuffleConnectors(Connector[] connectors) {
        for (int i = connectors.Length - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            (connectors[i], connectors[randomIndex]) = (connectors[randomIndex], connectors[i]);
        }
    }

    bool IsPieceSpaceAvailable(Vector3 worldPosition) {
        Vector2Int pieceGridPos = WorldToPieceGrid(worldPosition);
        return !occupiedSpaces.Contains(pieceGridPos);
    }

    GameObject GetEndPiecePrefab(ConnectorType connectorType) {
        switch (connectorType) {
            case ConnectorType.T: return endPieceDown;
            case ConnectorType.B: return endPieceUp;
            case ConnectorType.L: return endPieceRight;
            case ConnectorType.R: return endPieceLeft;
            default: return null;
        }
    }
}
