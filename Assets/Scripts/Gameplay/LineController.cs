using UnityEngine;
using System;
using Arrows.Data;
using Arrows.Spline;

namespace Arrows.Gameplay
{
    /// <summary>
    /// Manages a single colored line during gameplay: its state machine,
    /// arrowhead movement, partial line erasure, completion, and reset.
    ///
    /// State machine:
    ///   Idle → Moving (player clicks arrowhead)
    ///   Moving → Completed (arrowhead reaches destination)
    ///   Moving → Resetting (collision detected externally)
    ///   Resetting → Idle (instant reset)
    ///
    /// Movement is driven in Update only when state is Moving.
    /// The LineController does NOT detect collisions — that responsibility
    /// belongs to CollisionSystem (Stage 3).
    /// </summary>
    public class LineController : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        //  Configuration (set once during initialization)
        // ─────────────────────────────────────────────

        private LineDefinition definition;
        private SplinePath splinePath;

        // ─────────────────────────────────────────────
        //  Child components
        // ─────────────────────────────────────────────

        private SplineRenderer splineRenderer;
        private ArrowheadController arrowheadA;  // at t=0 (start)
        private ArrowheadController arrowheadB;  // at t=1 (end)
        private float arrowheadOriginalScale;

        // ─────────────────────────────────────────────
        //  Runtime movement state
        // ─────────────────────────────────────────────

        private LineState state = LineState.Idle;
        private ArrowheadController movingArrowhead;
        private float previousDistance;      // distance at start of frame
        private float currentDistance;       // distance along path (arc-length)
        private float targetDistance;        // distance at destination
        private float movementDirection;     // +1 (start→end) or -1 (end→start)

        // ─────────────────────────────────────────────
        //  Public properties
        // ─────────────────────────────────────────────

        /// <summary>Current state of this line.</summary>
        public LineState State => state;

        /// <summary>Unique identifier from the level data.</summary>
        public string LineId => definition?.lineId;

        /// <summary>Arc-length distance at start of current frame.</summary>
        public float PreviousDistance => previousDistance;

        /// <summary>Current arc-length distance along the spline.</summary>
        public float CurrentDistance => currentDistance;

        /// <summary>
        /// True if the moving arrowhead has reached its target destination in this movement cycle.
        /// Completion is NOT finalized until CollisionSystem has evaluated this final movement step.
        /// </summary>
        public bool HasReachedDestination { get; private set; }

        /// <summary>Start arrowhead (at t=0).</summary>
        public ArrowheadController ArrowheadA => arrowheadA;

        /// <summary>End arrowhead (at t=1).</summary>
        public ArrowheadController ArrowheadB => arrowheadB;

        /// <summary>The arrowhead currently moving (null if not in Moving state).</summary>
        public ArrowheadController MovingArrowhead => state == LineState.Moving ? movingArrowhead : null;

        /// <summary>The stationary arrowhead during movement (the destination).</summary>
        public ArrowheadController StationaryArrowhead =>
            state == LineState.Moving
                ? (movingArrowhead == arrowheadA ? arrowheadB : arrowheadA)
                : null;

        /// <summary>The SplinePath for this line (used by hint system for path simulation).</summary>
        public SplinePath Path => splinePath;

        /// <summary>Collision radius for this line's arrowheads.</summary>
        public float CollisionRadius => definition?.collisionRadius ?? 0.25f;

        // ─────────────────────────────────────────────
        //  Events
        // ─────────────────────────────────────────────

        /// <summary>Fired when this line is successfully completed (both arrowheads removed).</summary>
        public event Action<LineController> OnLineCompleted;

        /// <summary>Fired when movement starts on this line.</summary>
        public event Action<LineController> OnMovementStarted;

        // ─────────────────────────────────────────────
        //  Initialization
        // ─────────────────────────────────────────────

        /// <summary>
        /// Initializes the line with its data, spline, renderer, and arrowheads.
        /// Called by LevelLoader after all child objects are created.
        /// </summary>
        public void Initialize(
            LineDefinition def,
            SplinePath path,
            SplineRenderer renderer,
            ArrowheadController arrowA,
            ArrowheadController arrowB)
        {
            definition = def;
            splinePath = path;
            splineRenderer = renderer;
            arrowheadA = arrowA;
            arrowheadB = arrowB;
            arrowheadOriginalScale = def.arrowheadScale;

            // Initialize arrowheads with color and collision radius
            arrowA.Initialize(this, isStart: true, def.lineColor, def.collisionRadius);
            arrowB.Initialize(this, isStart: false, def.lineColor, def.collisionRadius);

            // Position arrowheads at their endpoints
            PositionArrowheadAtEndpoint(arrowA, 0f, facingSign: 1f);
            PositionArrowheadAtEndpoint(arrowB, 1f, facingSign: -1f);

            state = LineState.Idle;
        }

        /// <summary>
        /// Positions an arrowhead at a given spline parameter with the correct facing.
        /// facingSign: +1 = faces forward along path, -1 = faces backward.
        /// </summary>
        private void PositionArrowheadAtEndpoint(ArrowheadController arrowhead, float t, float facingSign)
        {
            Vector2 pos = splinePath.EvaluatePosition(t);
            Vector2 tangent = splinePath.EvaluateTangent(t);
            arrowhead.SnapToPosition(pos, tangent * facingSign);
        }

        // ─────────────────────────────────────────────
        //  Input
        // ─────────────────────────────────────────────

        /// <summary>
        /// Called by ArrowheadController when the player clicks an arrowhead.
        /// Starts movement from the clicked arrowhead toward the opposite end.
        /// </summary>
        public void OnArrowheadClicked(ArrowheadController clicked)
        {
            if (state != LineState.Idle) return;

            StartMovement(clicked);
        }

        // ─────────────────────────────────────────────
        //  Movement
        // ─────────────────────────────────────────────

        /// <summary>
        /// Begins arrowhead movement from the clicked arrowhead toward the opposite end.
        /// </summary>
        private void StartMovement(ArrowheadController source)
        {
            state = LineState.Moving;
            movingArrowhead = source;

            // Disable both arrowheads for input
            arrowheadA.SetInteractable(false);
            arrowheadB.SetInteractable(false);

            // Determine movement direction
            if (source == arrowheadA)
            {
                // Moving from start (distance=0) toward end (distance=totalLength)
                currentDistance = 0f;
                previousDistance = 0f;
                targetDistance = splinePath.TotalLength;
                movementDirection = 1f;
            }
            else
            {
                // Moving from end (distance=totalLength) toward start (distance=0)
                currentDistance = splinePath.TotalLength;
                previousDistance = splinePath.TotalLength;
                targetDistance = 0f;
                movementDirection = -1f;
            }

            HasReachedDestination = false;
            OnMovementStarted?.Invoke(this);
        }

        /// <summary>
        /// Deterministically advances the moving arrowhead along the spline by deltaTime.
        /// Called by GameManager during Phase 1 of the update pipeline.
        /// Does NOT finalize completion so that CollisionSystem can evaluate the final step.
        /// </summary>
        public void AdvanceMovement(float deltaTime)
        {
            if (state != LineState.Moving) return;

            previousDistance = currentDistance;
            currentDistance += definition.movementSpeed * deltaTime * movementDirection;

            bool reached = movementDirection > 0
                ? currentDistance >= targetDistance
                : currentDistance <= targetDistance;

            if (reached)
            {
                currentDistance = targetDistance;
                HasReachedDestination = true;
            }

            UpdateArrowheadVisuals();
        }

        /// <summary>
        /// Updates the moving arrowhead's position/rotation and the line's partial erasure.
        /// Eliminates any gaps at the boundaries by pinning the line directly to the moving arrowhead.
        /// </summary>
        private void UpdateArrowheadVisuals()
        {
            float t = splinePath.DistanceToParameter(currentDistance);
            Vector2 pos = splinePath.EvaluatePositionAtDistance(currentDistance);
            Vector2 tangent = splinePath.EvaluateTangentAtDistance(currentDistance);

            // Arrowhead faces in its movement direction
            movingArrowhead.UpdatePositionAndRotation(pos, tangent * movementDirection);

            Vector3 currentPos3D = new Vector3(pos.x, pos.y, 0f);

            // Partial line erasure: hide the portion behind the moving arrowhead
            if (movementDirection > 0)
            {
                // A→B: erase from start to current position, show from current to end
                Vector3 endPos3D = splinePath.EndPosition;
                splineRenderer.SetVisibleRange(t, 1f, currentPos3D, endPos3D);
            }
            else
            {
                // B→A: erase from current position to end, show from start to current
                Vector3 startPos3D = splinePath.StartPosition;
                splineRenderer.SetVisibleRange(0f, t, startPos3D, currentPos3D);
            }
        }

        // ─────────────────────────────────────────────
        //  Completion
        // ─────────────────────────────────────────────

        /// <summary>
        /// Called when the moving arrowhead successfully reaches its destination
        /// and collision evaluation has confirmed no arrival-step collision.
        /// Removes both arrowheads and the line permanently.
        /// </summary>
        public void CompleteLine()
        {
            state = LineState.Completed;
            HasReachedDestination = false;

            arrowheadA.Hide();
            arrowheadB.Hide();
            splineRenderer.Hide();

            movingArrowhead = null;

            OnLineCompleted?.Invoke(this);
        }

        // ─────────────────────────────────────────────
        //  Reset (called by CollisionSystem in Stage 3)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Immediately resets the line to its original state.
        /// Restores arrowheads to endpoints, restores full line rendering,
        /// and returns to Idle state.
        /// Called externally when a collision affects this line.
        /// </summary>
        public void ResetLine()
        {
            state = LineState.Resetting;

            // Restore arrowhead positions and scale
            arrowheadA.Show();
            arrowheadB.Show();
            arrowheadA.transform.localScale = Vector3.one * arrowheadOriginalScale;
            arrowheadB.transform.localScale = Vector3.one * arrowheadOriginalScale;

            PositionArrowheadAtEndpoint(arrowheadA, 0f, facingSign: 1f);
            PositionArrowheadAtEndpoint(arrowheadB, 1f, facingSign: -1f);

            arrowheadA.SetInteractable(true);
            arrowheadB.SetInteractable(true);

            // Restore line rendering
            splineRenderer.SetHighlighted(false);
            splineRenderer.SetFullyVisible();

            // Clear movement state
            movingArrowhead = null;
            currentDistance = 0f;
            previousDistance = 0f;
            HasReachedDestination = false;

            // Return to idle
            state = LineState.Idle;
        }

        // ─────────────────────────────────────────────
        //  Swept collision support (for Stage 3)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Returns the arc-length distance the moving arrowhead has covered this frame.
        /// Used by CollisionSystem for swept collision detection to prevent
        /// fast-moving arrowheads from passing through each other between frames.
        /// Returns 0 if not in Moving state.
        /// </summary>
        public float GetFrameDistanceDelta()
        {
            if (state != LineState.Moving) return 0f;
            return Mathf.Abs(definition.movementSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Returns the current distance along the path of the moving arrowhead.
        /// Returns -1 if not in Moving state.
        /// </summary>
        public float GetCurrentDistance()
        {
            return state == LineState.Moving ? currentDistance : -1f;
        }

        /// <summary>
        /// Returns the movement direction: +1 (start→end) or -1 (end→start).
        /// Returns 0 if not moving.
        /// </summary>
        public float GetMovementDirection()
        {
            return state == LineState.Moving ? movementDirection : 0f;
        }
    }
}
