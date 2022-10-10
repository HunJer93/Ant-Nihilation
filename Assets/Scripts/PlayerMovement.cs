using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float moveSpeed = 5f;

    public Rigidbody2D body;

    Vector2 movement;
    public Animator animator;

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

    }

    private void FixedUpdate()
    {
        // movment calculation
        body.MovePosition(body.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
