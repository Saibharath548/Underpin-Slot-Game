using UnityEngine;

namespace Arrows.Gameplay
{
    /// <summary>
    /// Static factory that creates and caches the arrowhead triangle mesh.
    /// The mesh is a simple triangle pointing in the +X direction, intended
    /// to be rotated to face the spline tangent at the arrowhead's position.
    /// </summary>
    public static class ArrowheadMeshFactory
    {
        private static Mesh cachedMesh;

        /// <summary>
        /// Returns a shared arrowhead mesh. Created once, reused for all arrowheads.
        /// </summary>
        public static Mesh GetMesh()
        {
            if (cachedMesh != null)
                return cachedMesh;

            cachedMesh = CreateTriangleMesh();
            return cachedMesh;
        }

        /// <summary>
        /// Creates a triangle mesh pointing right (+X direction).
        ///
        /// Shape:
        ///         tip (0.5, 0)
        ///        / |
        ///       /  |
        ///      /   |
        ///     /____|
        /// (-0.5, -0.35) to (-0.5, 0.35)
        /// </summary>
        private static Mesh CreateTriangleMesh()
        {
            var mesh = new Mesh { name = "Arrowhead" };

            // Triangle pointing right
            mesh.vertices = new Vector3[]
            {
                new Vector3( 0.5f,  0f,    0f),  // tip
                new Vector3(-0.5f,  0.35f, 0f),  // top-left
                new Vector3(-0.5f, -0.35f, 0f)   // bottom-left
            };

            // Two triangles (front and back) so the arrowhead is visible from both sides
            mesh.triangles = new int[]
            {
                0, 1, 2,  // front face
                0, 2, 1   // back face
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}

