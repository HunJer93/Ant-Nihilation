using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float moveSpeed = 5f;

    public Rigidbody2D body;

    Vector2 movement;

    // Update is called once per frame
    void Update()
    {
        // player movement input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        // movment calculation
        body.MovePosition(body.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
