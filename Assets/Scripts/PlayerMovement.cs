using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // PC model, position, and animator
    public Rigidbody2D body;
    private Vector2 movement;
    public Animator animator;

    // speed variables
    public float moveSpeed = 5f;
    public float dashSpeed = 50f;
    public float sprintSpeed = 10f;

    //bools for buttons
    private bool isDashButtonDown;
    private bool isSprintButtonDown;

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

            // if the user is sprinting, only have them dash
            if (isSprintButtonDown)
            {
                isSprintButtonDown = false;
            }

            // update animation
            animator.SetBool("IsDashing", isDashButtonDown);
        }

        // set shift for sprint
        if(Input.GetKey(KeyCode.LeftShift))
        {
            isSprintButtonDown = true;

            // update animation
            animator.SetBool("IsSprinting", isSprintButtonDown);
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
            body.MovePosition(body.position + movement * dashSpeed * Time.fixedDeltaTime);
            // update animation
            animator.SetBool("IsDashing", isDashButtonDown);

            // reset value and animation
            isDashButtonDown =false;
            animator.SetBool("IsDashing", isDashButtonDown);
        }

        // if the player is holding down the sprint button, they will sprint
        if (Input.GetKey(KeyCode.LeftShift))
        {
            // update motion and animation
            body.MovePosition(body.position + movement * sprintSpeed * Time.fixedDeltaTime);
            animator.SetBool("IsSprinting", isSprintButtonDown);

        }
        else
        {
            // reset value and animation
            isSprintButtonDown = false;
            animator.SetBool("IsSprinting", isSprintButtonDown);

        }
    }
}
