using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Sprite References")]
    [SerializeField] private Sprite frontSprite;  // S key - front view
    [SerializeField] private Sprite backSprite;   // W key - back view
    [SerializeField] private Sprite sideSprite;   // A/D key - side view

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactableLayer;

    // Component references
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private InteractionStateMachine stateMachine;

    // Movement variables
    private Vector2 movement;
    private bool isFacingRight = false;
    private bool movementEnabled = true;

    // Interaction variables
    private GameObject nearestInteractable;
    private IInteractable lastInteractedWith;
    private FacingDirection currentDirection = FacingDirection.Down;

    private enum FacingDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Add the state machine component 
        stateMachine = gameObject.AddComponent<InteractionStateMachine>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    private void Update()
    {
        if (movementEnabled)
        {
            HandleMovementInput();
        }
        else
        {
            movement = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (movementEnabled)
        {
            MoveCharacter();
        }
        else
        {
            // Stop movement during interaction
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    private void HandleMovementInput()
    {
        // Get input axis values
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Create movement vector
        movement = new Vector2(horizontal, vertical).normalized;

        // Update sprite based on movement direction
        UpdateSprite(horizontal, vertical);
    }

    private void UpdateSprite(float horizontal, float vertical)
    {
        // First check if we're moving
        if (horizontal == 0 && vertical == 0)
            return; // Keep the current sprite if not moving

        // Priority for changing sprites (vertical takes precedence over horizontal)
        if (vertical > 0)
        {
            // Moving up (W key) - show back view
            spriteRenderer.sprite = backSprite;
            spriteRenderer.flipX = false;
            currentDirection = FacingDirection.Up;
        }
        else if (vertical < 0)
        {
            // Moving down (S key) - show front view
            spriteRenderer.sprite = frontSprite;
            spriteRenderer.flipX = false;
            currentDirection = FacingDirection.Down;
        }
        else if (horizontal != 0)
        {
            // Moving horizontally (A/D keys) - show side view
            spriteRenderer.sprite = sideSprite;

            // Flip sprite horizontally based on direction
            // Default side sprite faces left, so flip when moving right
            spriteRenderer.flipX = horizontal > 0;
            isFacingRight = horizontal > 0;
            currentDirection = horizontal > 0 ? FacingDirection.Right : FacingDirection.Left;
        }
    }

    private void MoveCharacter()
    {
        // Apply movement using rigidbody for physics-based movement
        if (rb != null)
            rb.linearVelocity = movement * moveSpeed;
    }

    // Method to check if player has a nearby interactable
    public bool HasNearbyInteractable()
    {
        FindNearestInteractable();
        return nearestInteractable != null;
    }

    // Method to get the nearest interactable
    public GameObject GetNearestInteractable()
    {
        FindNearestInteractable();
        return nearestInteractable;
    }

    // Method to get the last interactable we interacted with
    public IInteractable GetLastInteractable()
    {
        return lastInteractedWith;
    }

    // Method to set the last interactable we interacted with
    public void SetLastInteractable(IInteractable interactable)
    {
        lastInteractedWith = interactable;
    }

    // Method to enable/disable movement
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    private void FindNearestInteractable()
    {
        // Find all interactables within range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionDistance, interactableLayer);

        float closestDistance = float.MaxValue;
        nearestInteractable = null;

        foreach (Collider2D collider in colliders)
        {
            float distance = Vector2.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestInteractable = collider.gameObject;
            }
        }
    }

    // Method to check if player is facing a specific interactable
    public bool IsFacingInteractable(GameObject interactable)
    {
        if (interactable == null)
            return false;

        // Calculate direction to interactable
        Vector2 directionToInteractable = (interactable.transform.position - transform.position).normalized;

        // Check if player is facing in this direction
        return IsFacingDirection(directionToInteractable);
    }

    public void FaceInteractable(Vector2 targetPosition)
    {
        // Calculate direction to interactable
        Vector2 direction = targetPosition - (Vector2)transform.position;

        // Determine which sprite to use based on direction
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal direction dominates
            spriteRenderer.sprite = sideSprite;
            spriteRenderer.flipX = direction.x > 0;
        }
        else if (direction.y > 0)
        {
            // Target is above
            spriteRenderer.sprite = backSprite;
            spriteRenderer.flipX = false;
        }
        else
        {
            // Target is below
            spriteRenderer.sprite = frontSprite;
            spriteRenderer.flipX = false;
        }
    }

    // Helper method to draw the interaction radius in the editor
    private void OnDrawGizmosSelected()
    {
        // Show interaction radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Draw facing direction
        Gizmos.color = Color.blue;
        Vector2 facingDir = GetFacingVector();
        Gizmos.DrawRay(transform.position, facingDir * 1.5f);
    }

    // Check if player is facing in a specific direction
    private bool IsFacingDirection(Vector2 direction)
    {
        // Get player's current facing direction as a vector
        Vector2 facingVector = GetFacingVector();

        // Calculate angle between facing direction and target direction
        float angle = Vector2.Angle(facingVector, direction);

        // Consider "facing" if within 45 degrees
        return angle < 45f;
    }

    // Convert facing direction enum to vector
    private Vector2 GetFacingVector()
    {
        switch (currentDirection)
        {
            case FacingDirection.Up:
                return Vector2.up;
            case FacingDirection.Down:
                return Vector2.down;
            case FacingDirection.Left:
                return Vector2.left;
            case FacingDirection.Right:
                return Vector2.right;
            default:
                return Vector2.down;
        }
    }
}