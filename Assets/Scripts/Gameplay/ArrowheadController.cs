using UnityEngine;

namespace Arrows.Gameplay
{
    /// <summary>
    /// Controls a single arrowhead — its visual appearance, position, rotation,
    /// and interactable state. This component does NOT handle its own movement;
    /// the parent LineController drives position updates during movement.
    ///
    /// Click detection is handled externally by GameManager's input system.
    /// This component exposes HandleClick() for the input system to call.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(CircleCollider2D))]
    public class ArrowheadController : MonoBehaviour
    {
        // References (set during initialization)
        private LineController parentLine;
        private MeshRenderer meshRenderer;
        private CircleCollider2D clickCollider;

        // Configuration
        private Color baseColor;
        private bool isStartArrowhead;
        private float collisionRadius = 0.25f;

        // State
        private bool isInteractable = true;

        private Vector2 previousPosition;

        // ─────────────────────────────────────────────
        //  Public properties
        // ─────────────────────────────────────────────

        /// <summary>World-space position of this arrowhead in current frame.</summary>
        public Vector2 Position => (Vector2)transform.position;

        /// <summary>World-space position from previous frame (used for swept collision detection).</summary>
        public Vector2 PreviousPosition => previousPosition;

        /// <summary>Whether this arrowhead can be clicked by the player.</summary>
        public bool IsInteractable => isInteractable;

        /// <summary>True if this arrowhead sits at the start (t=0) of its line.</summary>
        public bool IsStartArrowhead => isStartArrowhead;

        /// <summary>Reference to the line this arrowhead belongs to.</summary>
        public LineController ParentLine => parentLine;

        /// <summary>Gameplay collision radius for this arrowhead.</summary>
        public float CollisionRadius => collisionRadius;

        // ─────────────────────────────────────────────
        //  Initialization
        // ─────────────────────────────────────────────

        /// <summary>
        /// Initializes the arrowhead with its parent line, position type, color, and collision radius.
        /// </summary>
        /// <param name="parent">The LineController that owns this arrowhead.</param>
        /// <param name="isStart">True if at t=0 (start), false if at t=1 (end).</param>
        /// <param name="color">The color matching the parent line.</param>
        /// <param name="colRadius">The collision radius configured on the line.</param>
        public void Initialize(LineController parent, bool isStart, Color color, float colRadius)
        {
            parentLine = parent;
            isStartArrowhead = isStart;
            baseColor = color;
            collisionRadius = colRadius;

            meshRenderer = GetComponent<MeshRenderer>();
            clickCollider = GetComponent<CircleCollider2D>();

            // Set up mesh
            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = ArrowheadMeshFactory.GetMesh();

            // Set up material (Sprites/Default works with URP 2D)
            meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.material.color = baseColor;
            meshRenderer.sortingOrder = 1;

            // Configure CircleCollider2D to match collisionRadius in world units
            float scale = Mathf.Max(transform.localScale.x, 0.001f);
            clickCollider.radius = collisionRadius / scale;
            clickCollider.isTrigger = true;
        }

        // ─────────────────────────────────────────────
        //  Position and rotation
        // ─────────────────────────────────────────────

        /// <summary>
        /// Updates the arrowhead's world position and rotation to face the given direction.
        /// The arrowhead mesh points in +X, so we rotate it to match the tangent.
        /// </summary>
        /// <param name="position">World-space position.</param>
        /// <param name="facingDirection">Direction the arrowhead tip should point toward.</param>
        public void UpdatePositionAndRotation(Vector2 position, Vector2 facingDirection)
        {
            previousPosition = (Vector2)transform.position;
            transform.position = new Vector3(position.x, position.y, -0.1f);

            if (facingDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>
        /// Instantly sets position and clears previous position history (e.g. on reset).
        /// </summary>
        public void SnapToPosition(Vector2 position, Vector2 facingDirection)
        {
            transform.position = new Vector3(position.x, position.y, -0.1f);
            previousPosition = position;

            if (facingDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        // ─────────────────────────────────────────────
        //  Interactability
        // ─────────────────────────────────────────────

        /// <summary>
        /// Enables or disables player interaction with this arrowhead.
        /// Preserves the original vibrant color at all times.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;

            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = baseColor;
            }

            // Disable collider so raycasts skip it
            if (clickCollider != null)
                clickCollider.enabled = interactable;
        }

        // ─────────────────────────────────────────────
        //  Visibility
        // ─────────────────────────────────────────────

        /// <summary>Shows the arrowhead.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Hides the arrowhead.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────
        //  Highlight (hint system)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Sets the highlight state for the hint system.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (meshRenderer == null || meshRenderer.material == null) return;

            if (highlighted)
            {
                meshRenderer.material.color = Color.white;
                transform.localScale *= 1.3f;
            }
            else
            {
                meshRenderer.material.color = baseColor;
            }
        }

        // ─────────────────────────────────────────────
        //  Input
        // ─────────────────────────────────────────────

        /// <summary>
        /// Called by the input system when this arrowhead is clicked/tapped.
        /// Forwards the click to the parent LineController.
        /// </summary>
        public void HandleClick()
        {
            if (!isInteractable) return;
            if (parentLine == null) return;

            parentLine.OnArrowheadClicked(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
    }
}
