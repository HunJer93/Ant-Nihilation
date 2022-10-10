using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float moveSpeed = 5f;
    public float dashAmount = 50f;

    public Rigidbody2D body;
    private Vector2 movement;
    public Animator animator;
    private bool isDashButtonDown;

    // Update is called once per frame
    void Update()
    {
        // player movement input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);

        // set last motion moved
        if(Input.GetAxisRaw("Horizontal") == 1 || Input.GetAxisRaw("Horizontal") == -1 || Input.GetAxisRaw("Vertical") == 1 || Input.GetAxisRaw("Vertical") == -1)
        {
            animator.SetFloat("LastMoveHor", Input.GetAxisRaw("Horizontal"));
            animator.SetFloat("LastMoveVert", Input.GetAxisRaw("Vertical"));
        }

        // set spacebar down for dash button
        if(Input.GetKeyDown(KeyCode.Space))
        {
            isDashButtonDown = true;

            // update animation
            animator.SetBool("IsDashing", isDashButtonDown);
        }

    }

    private void FixedUpdate()
    {
        // movment calculation
        body.MovePosition(body.position + movement * moveSpeed * Time.fixedDeltaTime);

        // if the player presses dash button, roll
        if (isDashButtonDown)
        {
            // update motion
            body.MovePosition(body.position + movement * moveSpeed * dashAmount * Time.fixedDeltaTime);
            // update animation
            animator.SetBool("IsDashing", isDashButtonDown);

            // reset value and animation
            isDashButtonDown =false;
            animator.SetBool("IsDashing", isDashButtonDown);
        }
    }
}
