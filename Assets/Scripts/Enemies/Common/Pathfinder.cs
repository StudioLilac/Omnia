using System.Collections.Generic;
using System.Linq;
using Omnia.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Utils;
using Cell2D = UnityEngine.Vector2Int;
using Cell3D = UnityEngine.Vector3Int;

namespace Enemies.Common {
    public class Pathfinder : PersistentSingleton<Pathfinder> {
        [SerializeField] internal LayerMask solid;

        private readonly HashSet<Cell2D> obstacles = new();
        private Tilemap[] tiles;

        public void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        protected override void OnAwake() {
            tiles = FindObjectsByType<Tilemap>().Where(it => CollisionUtils.IsLayerInMask(it.gameObject.layer, solid)).ToArray();
            obstacles.Clear();
            foreach (var p in CompressedBoundsAllPositions(tiles)) {
                var p2 = (Cell2D)p;
                if (tiles.Any(it => it.HasTile(p))) obstacles.Add(p2);
            }
        }

        public void UpdateTilemap() {
            OnAwake();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            OnAwake();
        }

        private List<Vector3> DoFindPath(Vector3 a, Vector3 b) {
            if (tiles.Length == 0) return new List<Vector3>();
            var basis = tiles[0];

            var a0 = (Cell2D)basis.WorldToCell(a);
            var b0 = (Cell2D)basis.WorldToCell(b);
            return AStar(a0, b0).ConvertAll(it => basis.GetCellCenterWorld((Cell3D)it));
        }

        private List<Cell2D> AStar(Cell2D a, Cell2D b) {
            var predecessor = new Dictionary<Cell2D, Cell2D>();
            var gs = new Dictionary<Cell2D, float> { [a] = 0 };
            var fs = new Dictionary<Cell2D, float> { [a] = Heuristic(a, b) };

            var todo = new Heapish<Cell2D>((c1, c2) => Mathf.Approximately(fs[c1], fs[c2]) ? gs[c1] < gs[c2] : fs[c1] < fs[c2]);
            todo.Push(a);
            var visited = new HashSet<Cell2D>();

            while (todo.Size != 0) {
                var c = todo.Pop();
                if (c == b) return ReconstructPath(predecessor, b);
                visited.Add(c);

                foreach (var n in Next(c)) {
                    if (visited.Contains(n) || obstacles.Contains(n)) continue;
                    var candidateScore = gs[c] + Cell2D.Distance(c, n);
                    if (gs.ContainsKey(n) && candidateScore >= gs[n]) continue;

                    fs[n] = candidateScore + Heuristic(n, b);
                    gs[n] = candidateScore;
                    todo.Push(n);
                    predecessor[n] = c;
                }
            }

            return new List<Cell2D>();
        }

        private static List<Cell2D> ReconstructPath(Dictionary<Cell2D, Cell2D> predecessor, Cell2D c) {
            var path = new List<Cell2D> { c };
            for (var p = c; predecessor.ContainsKey(p); p = predecessor[p]) path.Add(p);

            return path.Reverse<Cell2D>().ToList();
        }

        private static float Heuristic(Cell2D a, Cell2D b) => Cell2D.Distance(a, b);

        private static Cell2D[] Next(Cell2D c) => new[] {
            new Cell2D(c.x + 1, c.y),
            new Cell2D(c.x - 1, c.y),
            new Cell2D(c.x, c.y + 1),
            new Cell2D(c.x, c.y - 1)
        };

        private static BoundsInt.PositionEnumerator CompressedBoundsAllPositions(Tilemap[] tiles) {
            if (tiles.Length == 0) return new BoundsInt.PositionEnumerator();

            var bounds = tiles[0].cellBounds;
            foreach (var t in tiles) {
                t.CompressBounds();
                bounds.SetMinMax(Cell3D.Min(bounds.min, t.cellBounds.min), Cell3D.Max(bounds.max, t.cellBounds.max));
            }

            return bounds.allPositionsWithin;
        }

        public static List<Vector3> FindPath(Vector2 a, Vector2 b) => Instance.DoFindPath(a, b);
    }
}
