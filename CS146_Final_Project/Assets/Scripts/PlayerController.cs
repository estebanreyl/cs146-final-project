﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
    // Movement parameters
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    private Rigidbody2D rb;
    // Ground status
    [SerializeField] private Transform[] groundPoints;
    [SerializeField] private float groundRadius;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private bool airControl;
    // Audio options
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private AudioClip shieldSound;
    // UI
    [SerializeField] private Text scoreText;
    public int pickupCount = 0;
    // Player status
    private bool facingRight;
    private bool hasBall;
    private bool isGrounded;
    private bool jump;
    private bool throwBall;
    private bool shield;
    private bool isDead;
    // Animation
    [SerializeField] private Animator anim;
    // Player Systems
    [SerializeField] private GameObject forceField;

    /* Init vars. */
    void Start () {
        rb = GetComponent<Rigidbody2D>();
        facingRight = true;
        hasBall = true;
        throwBall = false;
        isDead = false;
        forceField.SetActive(false);
    }

    /* Check for input. */
    void Update()
    {
        if (isDead) return;

        // Read the jump input in Update so button presses aren't missed.
        if (!jump) jump = Input.GetButtonDown("Jump");
        if (!throwBall) throwBall = Input.GetButtonDown("Fire1");
        if (!shield) shield = Input.GetButton("Fire2");
    }

    /* Compute physics and movement. */
    void FixedUpdate () {
        if (isDead) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        isGrounded = getIsGrounded();
        HandleMovement(horizontal, vertical);
        Flip(horizontal);
        jump = false; // reset input
        throwBall = false;
        shield = false;
    }

    /* Handle all forms of player movement. */
    private void HandleMovement(float horizontal, float vertical)
    {
        // Set the vertical animation
        anim.SetFloat("vSpeed", rb.velocity.y);

        // Set grounded animation
        anim.SetBool("isGrounded", isGrounded);

        // Set throwing
        if (throwBall && hasBall)
        {
            anim.SetBool("isThrowing", true);
			source.PlayOneShot(throwSound);
            hasBall = false;
        }
        else
        {
            anim.SetBool("isThrowing", false);
        }

        // Set shielding - TODO: put on cooldown, add collider?
        if (shield && hasBall && isGrounded)
        {
            forceField.SetActive(true);
            source.PlayOneShot(shieldSound);
            anim.SetBool("isShielding", true);
        }
        else
        {
            forceField.SetActive(false);
            anim.SetBool("isShielding", false);
        }

        // Disbale movement if shielding
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Shield") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("Throw")) horizontal = 0.0f;

        // Set movement
        if (isGrounded || airControl) rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        anim.SetFloat("hSpeed", Mathf.Abs(horizontal));

        // Set jumping
        if (isGrounded && jump && anim.GetBool("isGrounded"))
        {
            source.PlayOneShot(jumpSound);
            isGrounded = false;
            anim.SetBool("isGrounded", false);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Force);
        }
    }

    /* Flips player facing direction */
    private void Flip(float horizontal)
    {
        // Set movement direction
        if (horizontal > 0 && !facingRight || horizontal < 0 && facingRight)
        {
            facingRight = !facingRight;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }

    /* Handles trigger collisions with the player */ 
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "KillPlane" || collision.tag == "Bullet")
        {
            anim.SetBool("isDead", true);
            isDead = true;
            FindObjectOfType<GameManager>().endGame();
        }
    }

    /* Pickup dodgeball. */
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Ball")
        {
            // Pickup ball - TODO: perform other actions?
            hasBall = true;
        }
    }

    /* Checks whether the player is grounded */
    private bool getIsGrounded()
    {
        if (rb.velocity.y <= 0)
        {
            foreach(Transform point in groundPoints)
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(point.position, groundRadius, whatIsGround);

                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].gameObject != gameObject && colliders[i].tag != "Climbable")
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /* Play pickup sound and updates score */ 
    public void playPickupSound()
    {
        updateScore(1);
        source.PlayOneShot(pickupSound);
    }

    /* Adds value to score and updates UI component */
    public void updateScore(int add)
    {
        pickupCount += add;
        scoreText.text = pickupCount.ToString();
    }
}