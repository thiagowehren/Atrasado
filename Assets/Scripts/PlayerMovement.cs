using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D _rb;
    private Vector3 objectScale;
    private BoxCollider2D boxCollider;

    [Header("Movement Variables")]
    [SerializeField] private float _movementAcceleration = 2f;
    [SerializeField] private float _walkSpeed = 7f;
    [SerializeField] private float _currentMovementLerpSpeed = 100f;
    private bool _changingDirection => (_rb.velocity.x > 0f && _horizontalDirection < 0f) || (_rb.velocity.x < 0f && _horizontalDirection > 0f);

    [Header("Layer Masks")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _wallLayer;

    [Header("Jump Variables")]
    [SerializeField] private float _jumpForce = 23f;
    [SerializeField] private float _fallMultiplier = 7f;
    [SerializeField] private float _jumpVelocityFalloff = 7f;
    [SerializeField] private float _hangTime = 0.1f;
    private float _hangTimeCounter;
    [SerializeField] private float _jumpBufferLength = 0.5f;
    private float _jumpBufferCounter;
    [SerializeField] private bool _canJump => _jumpBufferCounter > 0f && _hangTimeCounter > 0f;
    [SerializeField] private float _coyoteTime = 0.2f;
    private float _timeLeftGrounded = -10;
    private float _timeLastWallJumped;
    private bool _hasJumped;
    [SerializeField] private Transform _jumpLaunchPoof;

    [Header("Ground Collision Variables")]
    private bool _onGround;
    private bool IsGrounded;
    private LayerMask _groundMask;
    [SerializeField] private float _grounderOffset = -1, _grounderRadius = 0.2f;

    [Header("Wall Collision Variables")]
    [SerializeField] private float _wallCheckOffset = 0.3f, _wallCheckRadius = 0.1f;
    private bool _isAgainstLeftWall, _isAgainstRightWall, _pushingLeftWall, _pushingRightWall;
    private readonly Collider[] _ground = new Collider[1];
    private readonly Collider[] _leftWall = new Collider[1];
    private readonly Collider[] _rightWall = new Collider[1];
    
    [Header("Wall Slide Variables")]
    [SerializeField] private float _slideSpeed = 1;
    [SerializeField] private bool _wallSliding;

    [Header("Wall Grab")] [SerializeField]
    private bool _grabbing;

    [Header("Animation Variables")]
    private Animator _anim;

    [Header("Dash Variables")] 
    [SerializeField] private float _dashSpeed = 15;
    [SerializeField] private float _dashLength = 0.2f;
    [SerializeField] private ParticleSystem _dashParticles;
    [SerializeField] private Transform _dashRing;
    [SerializeField] private ParticleSystem _dashVisual;
    private bool _hasDashed;
    private bool _dashing;
    private float _timeStartedDash;
    private Vector3 _dashDir;


    [SerializeField] private float _wallJumpLock = 0.25f;
    [SerializeField] private float _wallJumpMovementLerp = 2;


    [Header("Input Variables")]
    private float _horizontalDirection;
    private float _verticalDirection;
    private float _horizontalRawDirection;
    private float _verticalRawDirection;
    private bool _facingLeft => (transform.localScale.x < 0f);

    public static event Action OnStartDashing, OnStopDashing;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f; 
        objectScale = transform.localScale;
        boxCollider = GetComponent<BoxCollider2D>();
        _anim = GetComponent<Animator>();
        _groundLayer = LayerMask.GetMask("Ground");
        _wallLayer = LayerMask.GetMask("Wall");
    }

    private void Update()
    {
        _horizontalDirection = GetInput().x;
        _verticalDirection = GetInput().y;
        _horizontalRawDirection = (int) Input.GetAxisRaw("Horizontal");
        _verticalRawDirection = (int) Input.GetAxisRaw("Vertical");
        
        if (Input.GetButton("Jump") || (Input.GetButton("Jump_W") && !_grabbing))
        {
            // Debug.Log("buffering");
            _jumpBufferCounter = _jumpBufferLength;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        MoveCharacter();
        Flip();
        HandleDashing();   
        HandleGrounding();
        HandleWallGrab();
        // HandleWallSlide();
        HandleJumping();

        if (!isGrounded())
        {
            _hangTimeCounter -= Time.deltaTime;
        }
        else
        {
            _hasDashed = false;
            _hangTimeCounter = _hangTime;
        }
    }

    private static Vector2 GetInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    private void OnDrawGizmos()
    {
        // Gizmos.color = Color.red;
        // Gizmos.DrawWireSphere((Vector2)transform.position + new Vector2(-_wallCheckOffset, 0), _wallCheckRadius);
        // Gizmos.DrawWireSphere((Vector2)transform.position + new Vector2(_wallCheckOffset, 0), _wallCheckRadius);

        // Draw raycast for left wall
        Gizmos.color = Color.red;
        Vector2 originLeft = transform.position;
        Vector2 directionLeft = Vector2.left;
        float distanceLeft = _wallCheckRadius;
        Gizmos.DrawRay(originLeft, directionLeft * distanceLeft);

        // Draw raycast for right wall
        Gizmos.color = Color.blue;
        Vector2 originRight = transform.position;
        Vector2 directionRight = Vector2.right;
        float distanceRight = _wallCheckRadius;
        Gizmos.DrawRay(originRight, directionRight * distanceRight);
    }

    private void HandleGrounding() 
    {
        // Grounder
        var grounded = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(0, _grounderOffset), _grounderRadius, _ground, _groundMask) > 0;
        if(grounded)
            Debug.Log("grounded : "+grounded);
        if (isGrounded() == true) {
            IsGrounded = true;
            _hasDashed = false;
            _hasJumped = false;
            _currentMovementLerpSpeed = 100;
            // _anim.SetBool("Grounded", true);
            // transform.SetParent(_ground[0].transform);
        }
        else if (isGrounded() == false) {
            IsGrounded = false;
            _timeLeftGrounded = Time.time;
            // _anim.SetBool("Grounded", false);
            if(_isAgainstRightWall || _isAgainstLeftWall){
                _dashing = false;
            }
        }
        // Wall detection
        // var leftWallHits = Physics2D.OverlapCircleAll(transform.position + new Vector3(-_wallCheckOffset, 0), _wallCheckRadius, _groundMask);
        // var rightWallHits = Physics2D.OverlapCircleAll(transform.position + new Vector3(_wallCheckOffset, 0), _wallCheckRadius, _groundMask);
        // if(IsWallToLeft() || IsWallToRight())
        //     Debug.Log("wall hit:" + IsWallToLeft() + " " +  IsWallToRight());
        _isAgainstLeftWall = IsWallToLeft();
        _isAgainstRightWall = IsWallToRight();
        _pushingLeftWall = _isAgainstLeftWall && _horizontalDirection < 0;
        _pushingRightWall = _isAgainstRightWall && _horizontalDirection > 0;
    }

    private bool IsWallToLeft()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.left, _wallCheckRadius, _wallLayer);
        return raycastHit.collider != null;
    }

    private bool IsWallToRight()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.right, _wallCheckRadius, _wallLayer);
        return raycastHit.collider != null;
    }
    
    private void HandleJumping()
    {
        if (_dashing) return; //11
        if (Input.GetButton("Jump"))
        {
            if (_grabbing || (!isGrounded() && (_isAgainstLeftWall || _isAgainstRightWall)))
            {
            // Debug.Log("TRIED: _grabbing || (!isGrounded() && (_isAgainstLeftWall || _isAgainstRightWall))");
                _timeLastWallJumped = Time.time;
                _currentMovementLerpSpeed = _wallJumpMovementLerp;
                ExecuteJump(new Vector2(_isAgainstLeftWall ? _jumpForce/2 : -_jumpForce/2, _jumpForce/2)); // Wall jump
            }
            else if (isGrounded() || (Time.time < _timeLeftGrounded + _coyoteTime))
            {
            // Debug.Log("TRIED 2 : _hasjumped):" +_hasJumped);
                if(_hasJumped == false){
                    ExecuteJump(new Vector2(_rb.velocity.x, _jumpForce)); // Ground jump
                }
            }
        }
        else if(Input.GetButton("Jump_W"))
        {
            if (isGrounded() || (Time.time < _timeLeftGrounded + _coyoteTime)){
                if(_hasJumped == false){
                    ExecuteJump(new Vector2(_rb.velocity.x, _jumpForce)); // Ground jump
                }
            }
        }

        void ExecuteJump(Vector3 dir)
        {
            _rb.velocity = dir;
            // _anim.SetTrigger("Jump");
            _hasJumped = true;
        }

        // Fall faster and allow small jumps. _jumpVelocityFalloff is the point at which we start adding extra gravity. Using 0 causes floating
        if (_rb.velocity.y < _jumpVelocityFalloff || (_rb.velocity.y > 0 && !Input.GetKey(KeyCode.C)))
            _rb.velocity += _fallMultiplier * Physics.gravity.y * Vector2.up * Time.deltaTime;
    }

    private void HandleDashing() 
    {
        // if (_grabbing) return;
        if (Input.GetButton("Dash") && !_hasDashed) {
            _dashDir = new Vector3(_horizontalRawDirection, _verticalRawDirection).normalized;
            if (_dashDir == Vector3.zero) _dashDir = _facingLeft ? Vector2.left : Vector2.right;
            _dashing = true;
            _hasDashed = true;
            _timeStartedDash = Time.time;
            _rb.gravityScale = 0f;
        }

        if (_dashing) {
            _rb.velocity = _dashDir * _dashSpeed;

            if (Time.time >= _timeStartedDash + _dashLength) {
                // _dashParticles.Stop();
                _dashing = false;
                // Clamp the velocity so they don't keep shooting off
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y > 3 ? 2 : _rb.velocity.y);
                _rb.gravityScale = 1f;
                if (isGrounded()) _hasDashed = false;
            }
        }
    }
    
    private void HandleWallSlide() 
    {
        var sliding = _pushingLeftWall || _pushingRightWall;

        if (sliding && !_wallSliding) {
            transform.SetParent(_pushingLeftWall ? _leftWall[0].transform : _rightWall[0].transform);
            _wallSliding = true;

            // Don't add sliding until actually falling or it'll prevent jumping against a wall
            if (_rb.velocity.y < 0) _rb.velocity = new Vector3(0, -_slideSpeed);
        }
        else if (!sliding && _wallSliding && !_grabbing) {
            transform.SetParent(null);
            _wallSliding = false;
        }
    }

    private void HandleWallGrab() 
    {
        // I added wallJumpLock but I honestly can't remember why and I'm too scared to remove it...
        var grabbing = (_isAgainstLeftWall || _isAgainstRightWall) && Time.time > _timeLastWallJumped + _wallJumpLock;
        // Debug.log("LEFT:"+_isAgainstLeftWall+ "RIGHT:" + _isAgainstRightWall)
        _rb.gravityScale = !_grabbing && !isGrounded() ? 1f : 0f;
        if (grabbing && !_grabbing) {
            _grabbing = true;
        }
        else if (!grabbing && _grabbing) {
            _grabbing = false;
        }

        if (_grabbing) _rb.velocity = new Vector3(0, _verticalRawDirection * _slideSpeed * (_verticalRawDirection < 0 ? 1 : 0.8f));

        // _anim.SetBool("Climbing", _wallSliding || _grabbing);
    }

    private bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, _groundLayer);
        _onGround = raycastHit;
        return raycastHit.collider != null;
    }

    private void MoveCharacter()
    {
        // Slowly release control after wall jump
        _currentMovementLerpSpeed = Mathf.MoveTowards(_currentMovementLerpSpeed, 100, _wallJumpMovementLerp * Time.deltaTime);

        if (_dashing) return;

        var acceleration = isGrounded() ? _movementAcceleration : _movementAcceleration * 0.5f;

        if (_horizontalDirection < 0)
        {
            if (_rb.velocity.x > 0) _horizontalDirection = 0;
            _horizontalDirection = Mathf.MoveTowards(_horizontalDirection, -1, acceleration * Time.deltaTime);
        }
        else if (_horizontalDirection > 0)
        {
            if (_rb.velocity.x < 0) _horizontalDirection = 0;
            _horizontalDirection = Mathf.MoveTowards(_horizontalDirection, 1, acceleration * Time.deltaTime);
        }
        else
        {
            _horizontalDirection = Mathf.MoveTowards(_horizontalDirection, 0, acceleration * 2 * Time.deltaTime);
        }

        var idealVel = new Vector3(_horizontalDirection * _walkSpeed, _rb.velocity.y);
        _rb.velocity = Vector3.MoveTowards(_rb.velocity, idealVel, _currentMovementLerpSpeed * Time.deltaTime);

        // _anim.SetBool("isRunning", _horizontalRawDirection != 0 && isGrounded());
    }

    private void Fall()
    {
        // Fall faster and allow small jumps. _jumpVelocityFalloff is the point at which we start adding extra gravity. Using 0 causes floating
        if (_rb.velocity.y < _jumpVelocityFalloff || _rb.velocity.y > 0){
            _rb.velocity += _fallMultiplier * Physics.gravity.y * Vector2.up * Time.deltaTime;
        }
    }

    private void Flip()
    {
        if (_horizontalDirection > 0.01f)
            transform.localScale = new Vector3(objectScale.x, objectScale.y, objectScale.z);
        else if (_horizontalDirection < -0.01f)
            transform.localScale = new Vector3(-objectScale.x, objectScale.y, objectScale.z);
    }

    private void Jump()
    {
        _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        _hangTimeCounter = 0f;
        _jumpBufferCounter = 0f;
    }
}
