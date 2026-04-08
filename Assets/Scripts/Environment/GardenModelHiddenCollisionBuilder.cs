using UnityEngine;

namespace ZhuozhengYuan
{
    [DisallowMultipleComponent]
    public class GardenModelHiddenCollisionBuilder : MonoBehaviour
    {
        public Transform targetRoot;
        public string fallbackRootName = "GardenModel";
        public bool includeInactiveChildren = true;
        public bool skipObjectsThatAlreadyHaveCollider = true;
        public bool skipObjectsWithRigidbody = true;
        public bool disableFallbackGroundCollider = true;
        public string fallbackGroundObjectName = "GlobalInvisibleGround";
        public bool logBuildSummary = true;

        [SerializeField]
        private int lastAddedColliderCount;

        public int EnsureHiddenColliders()
        {
            Transform resolvedRoot = ResolveTargetRoot();
            if (resolvedRoot == null)
            {
                Debug.LogWarning("GardenModelHiddenCollisionBuilder could not find a target root. Assign targetRoot or keep a scene object named GardenModel.");
                lastAddedColliderCount = 0;
                return 0;
            }

            int addedCount = 0;
            MeshFilter[] meshFilters = resolvedRoot.GetComponentsInChildren<MeshFilter>(includeInactiveChildren);

            for (int index = 0; index < meshFilters.Length; index++)
            {
                MeshFilter meshFilter = meshFilters[index];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                GameObject owner = meshFilter.gameObject;
                if (skipObjectsWithRigidbody && owner.GetComponentInParent<Rigidbody>() != null)
                {
                    continue;
                }

                MeshCollider existingMeshCollider = owner.GetComponent<MeshCollider>();
                if (existingMeshCollider != null)
                {
                    if (existingMeshCollider.sharedMesh != meshFilter.sharedMesh)
                    {
                        existingMeshCollider.sharedMesh = meshFilter.sharedMesh;
                    }

                    existingMeshCollider.convex = false;
                    existingMeshCollider.isTrigger = false;
                    continue;
                }

                if (skipObjectsThatAlreadyHaveCollider && owner.GetComponent<Collider>() != null)
                {
                    continue;
                }

                MeshCollider meshCollider = owner.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = false;
                addedCount++;
            }

            lastAddedColliderCount = addedCount;

            if (logBuildSummary)
            {
                Debug.Log("GardenModelHiddenCollisionBuilder added " + addedCount + " hidden mesh colliders under " + resolvedRoot.name + ".");
            }

            if (disableFallbackGroundCollider)
            {
                DisableFallbackGroundCollider();
            }

            return addedCount;
        }

        private Transform ResolveTargetRoot()
        {
            if (targetRoot != null)
            {
                return targetRoot;
            }

            GameObject foundRoot = GameObject.Find(fallbackRootName);
            if (foundRoot != null)
            {
                targetRoot = foundRoot.transform;
            }

            return targetRoot;
        }

        private void DisableFallbackGroundCollider()
        {
            if (string.IsNullOrEmpty(fallbackGroundObjectName))
            {
                return;
            }

            GameObject fallbackGround = GameObject.Find(fallbackGroundObjectName);
            if (fallbackGround == null)
            {
                return;
            }

            Collider[] colliders = fallbackGround.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null)
                {
                    colliders[index].enabled = false;
                }
            }

            if (logBuildSummary)
            {
                Debug.Log("GardenModelHiddenCollisionBuilder disabled fallback ground collider on " + fallbackGroundObjectName + ".");
            }
        }
    }
}
