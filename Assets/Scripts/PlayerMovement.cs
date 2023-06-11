using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D _rb;
    private Vector3 objectScale;
    private BoxCollider2D boxCollider;

    [Header("Movement Variables")]
    [SerializeField] private float _movementAcceleration = 10f;
    [SerializeField] private float _maxMoveSpeed = 12f;
    [SerializeField] private float _groundLinearDrag = 10f;
    private float _horizontalDirection;
    private bool _changingDirection => (_rb.velocity.x > 0f && _horizontalDirection < 0f) || (_rb.velocity.x < 0f && _horizontalDirection > 0f);

    [Header("Layer Masks")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Jump Variables")]
    [SerializeField] private float _jumpForce = 20f;
    [SerializeField] private float _airLinearDrag = 2.5f;
    [SerializeField] private float _fallMultiplier = 8f;
    [SerializeField] private float _lowJumpFallMultiplier = 10f;
    [SerializeField] private float _hangTime = 0.1f;
    private float _hangTimeCounter;
    [SerializeField] private float _jumpBufferLength = 0.1f;
    private float _jumpBufferCounter;
    [SerializeField] private bool _canJump => _jumpBufferCounter > 0f && _hangTimeCounter > 0f;

    [Header("Ground Collision Variables")]
    // [SerializeField] private float _groundRayCastLength;
    private bool _onGround;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        objectScale = transform.localScale;
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        // horizontal = Input.GetAxis("Horizontal");
        // vertical = Input.GetAxis("Vertical");
        // _rb.velocity = new Vector2(horizontal * sprintPower, _rb.velocity.y);
        _horizontalDirection = GetInput().x;
        if(Input.GetButton("Jump")){
            _jumpBufferCounter = _jumpBufferLength;
        } else {
            _jumpBufferCounter -= Time.deltaTime;
        }
        if(_canJump) Jump();
    }

    private void FixedUpdate() {
        MoveCharacter();
        Flip();
        ApplyGroundLinearDrag();  
        if(!isGrounded()){
            ApplyAirLinearDrag();      
            _hangTimeCounter -= Time.deltaTime;
        } else {
            _hangTimeCounter = _hangTime;
        }
        Fall();
    }

    private static Vector2 GetInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));
    }

    private void MoveCharacter()
    {
        _rb.AddForce(new Vector2 (_horizontalDirection * _movementAcceleration, 0f));

        if(Mathf.Abs(_rb.velocity.x) > _maxMoveSpeed)
            _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * _maxMoveSpeed, _rb.velocity.y);
    }

    private void ApplyGroundLinearDrag()
    {
        if(Mathf.Abs(_horizontalDirection) < 0.4f || _changingDirection)
            _rb.drag = _groundLinearDrag;
        else
            _rb.drag = 0f;
    }

    private void ApplyAirLinearDrag()
    {
        _rb.drag = _airLinearDrag;
    }

    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     if(collision.gameObject.tag == "Ground")
    //         airborne = true;
    // }

    private bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, _groundLayer);
        _onGround = raycastHit;
        return raycastHit.collider != null;
    }

    // private bool isGrounded(){
    //     RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
    //     return raycastHit.collider != null;
    // }

    private void Fall()
    {
        if(_rb.velocity.y < 0){
            // _rb.gravityScale = _fallMultiplier;
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (_fallMultiplier - 1 ) * Time.deltaTime;
        }
        else if(_rb.velocity.y > 0 && !Input.GetButton("Jump")){
            // _rb.gravityScale = _lowJumpFallMultiplier;
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (_fallMultiplier - 1) * Time.deltaTime;
        }
    }

    private void Flip()
    {
        if(_horizontalDirection > 0.01f)
            transform.localScale = new Vector3(objectScale.x,objectScale.y,objectScale.z);
        else if(_horizontalDirection < -0.01f)
            transform.localScale = new Vector3(-objectScale.x,objectScale.y,objectScale.z);
    }

    private void Jump()
    {
        _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        //_rb.velocity += Vector2.up * _jumpForce;
        _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        _hangTimeCounter = 0f;
        _jumpBufferCounter = 0f;
    }
}
