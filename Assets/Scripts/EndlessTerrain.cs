using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EndlessTerrain : MonoBehaviour
{
    static MapGenerator mapGenerator;
    static HUD hud;

    const float scale = 2f;

    const float viewerMoveThreSholdForChunkUpdate = 25f;
    const float sqrViewerMoveThreSholdForChunkUpdate = viewerMoveThreSholdForChunkUpdate * viewerMoveThreSholdForChunkUpdate;

    public static float maxViewDst;
    public LODInfo[] detailLevels;

    Transform viewer;
    public Transform cameraViewer;
    public Transform fpsViewer;
    public Transform tpsViewer;
    [HideInInspector] public bool isFpsViewer = false;
    [HideInInspector] public bool isTpsViewer = false;

    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    int chunkSize;
    int chunkVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    bool positionChange = false;
    bool parameterChange = false;
    bool isEndlessTerrainChanged = false;

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        hud = FindObjectOfType<HUD>();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        JustUpdateChunk();

        ChangeViewer();
    }

    private void Update()
    {
        ChangeViewer();
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/ scale;

        if (mapGenerator.isEndlessTerrain)
        {
            if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThreSholdForChunkUpdate)
            {
                positionChange = true;
                viewerPositionOld = viewerPosition;
                UpdateVisibleChunks();
            }

            if (mapGenerator.lastFrameOctaves != mapGenerator.octaves || mapGenerator.lastFramePersistance != mapGenerator.persistance || mapGenerator.lastFrameLacunarity != mapGenerator.lacunarity || mapGenerator.lastFrameHeightMultiplier != mapGenerator.meshHeightMultiplier
                || mapGenerator.lastFrameNoiseScale != mapGenerator.noiseScale || mapGenerator.lastFrameOffset != mapGenerator.offset || mapGenerator.lastFrameSeed != mapGenerator.seed || mapGenerator.lastFrameUseFalloff != mapGenerator.useFalloff || mapGenerator.lastFrameEditorPreviewLOD != mapGenerator.editorPreviewLOD)
            {
                parameterChange = true;

                mapGenerator.lastFrameOctaves = mapGenerator.octaves;
                mapGenerator.lastFramePersistance = mapGenerator.persistance;
                mapGenerator.lastFrameLacunarity = mapGenerator.lacunarity;
                mapGenerator.lastFrameHeightMultiplier = mapGenerator.meshHeightMultiplier;
                mapGenerator.lastFrameNoiseScale = mapGenerator.noiseScale;
                mapGenerator.lastFrameOffset = mapGenerator.offset;
                mapGenerator.lastFrameSeed = mapGenerator.seed;
                mapGenerator.lastFrameEditorPreviewLOD = mapGenerator.editorPreviewLOD;
                mapGenerator.lastFrameUseFalloff = mapGenerator.useFalloff;
                //Debug.Log("update chunks");
                UpdateVisibleChunks();
            }
        }




        if (!mapGenerator.isEndlessTerrain && !isEndlessTerrainChanged)
        {
            isEndlessTerrainChanged = true;
            Debug.Log("isEndlessTerrainHide");
            foreach (TerrainChunk chunk in terrainChunkDictionary.Values)
            {
                Destroy(chunk.meshObject);
                chunk.meshObject = null;
            }
            terrainChunkDictionary.Clear();
            terrainChunksVisibleLastUpdate.Clear();
        }
        else if (mapGenerator.isEndlessTerrain)
        {
            isEndlessTerrainChanged = false;
        }

    }

    void ChangeViewer()
    {
        if (!isTpsViewer && !isFpsViewer)
        {
            viewer = cameraViewer;
            fpsViewer.parent.gameObject.SetActive(false);
            tpsViewer.parent.gameObject.SetActive(false);



        }

        if (isFpsViewer && !isTpsViewer)
        {
            viewer = fpsViewer;
            fpsViewer.parent.gameObject.SetActive(true);
            tpsViewer.parent.gameObject.SetActive(false);

        }

        if (isTpsViewer)
        {
            viewer = tpsViewer;           
            fpsViewer.parent.gameObject.SetActive(false);
            tpsViewer.parent.gameObject.SetActive(true);

        }
    }

    public void UpdateVisibleChunks()
    {

        if (parameterChange)
        {
            Debug.Log("new chunks");

            foreach (TerrainChunk chunk in terrainChunkDictionary.Values)
            {
                Destroy(chunk.meshObject);
                chunk.meshObject = null;

            }
            terrainChunkDictionary.Clear();
            terrainChunksVisibleLastUpdate.Clear();

            int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

            JustUpdateChunk();

            parameterChange = false;

        }


        if (positionChange)
        {
            Debug.Log("update chunks");

            for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
            {
                terrainChunksVisibleLastUpdate[i].SetVisible(false);
            }
            terrainChunksVisibleLastUpdate.Clear();

            JustUpdateChunk();

            positionChange = false;
        }

       
    }

    public void JustUpdateChunk()
    {
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunkVisibleInViewDst; yOffset <= chunkVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunkVisibleInViewDst; xOffset <= chunkVisibleInViewDst; xOffset++)
            {
                Vector2 viewdChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewdChunkCoord))
                {
                    terrainChunkDictionary[viewdChunkCoord].UpdateTerrainChunk();

                }
                else
                {
                    terrainChunkDictionary.Add(viewdChunkCoord, new TerrainChunk(viewdChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }

        }
    }




    [RequireComponent(typeof(MeshRenderer),typeof(MeshFilter),typeof(MeshCollider))]
    public class TerrainChunk
    {
        public GameObject meshObject;
        Vector2 position;
        Bounds bounds;  //包围盒

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLodMesh;

        public MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

   

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            lodMeshes = new LODMesh[detailLevels.Length];

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");

            if (meshObject == null)
            {
                return;
            }
            else
            {
                meshRenderer = meshObject.AddComponent<MeshRenderer>();
                meshFilter = meshObject.AddComponent<MeshFilter>();
                meshCollider = meshObject.AddComponent<MeshCollider>();
                meshRenderer.material = material;

                meshObject.transform.position = positionV3 * scale;
                meshObject.transform.parent = parent;
                meshObject.transform.localScale = Vector3.one * scale;
                SetVisible(false);

                for (int i = 0; i < detailLevels.Length; i++)
                {
                    lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                    if (detailLevels[i].useForCollider)
                    {
                        collisionLodMesh = lodMeshes[i];
                    }
                }

                mapGenerator.RequestMapData(position, OnMapDataReceived);
            }

            

            
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);

            if (meshObject!=null)
            {
                meshRenderer.material.mainTexture = texture;
            }

            UpdateTerrainChunk();
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;

                            if (meshObject != null)
                            {
                                meshFilter.mesh = lodMesh.mesh;
                            }
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);

                        }
                    }

                    if (lodIndex == 0)
                    {
                        if (collisionLodMesh.hasMesh && meshObject != null)
                        {
                            meshCollider.sharedMesh = collisionLodMesh.mesh;
                        }
                        else if (!collisionLodMesh.hasRequestedMesh)
                        {
                            collisionLodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }
            
        }

        public void SetVisible(bool visible)
        {
            if (meshObject != null)
            {
                meshObject.SetActive(visible);
            }

        }

        public bool isVisible()
        {
            if (meshObject != null)
            {
                return meshObject.activeSelf;
            }
            else
            {
                return false;
            }

        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;   //由于UpdateVisibleChunks()不再每帧调用，地形不会随着viwer的移动而实时更新，当接受MapData和MeshData时要手动更新

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold;   //超出观察阈值就会切换到next lod
        public bool useForCollider;
    }

}
