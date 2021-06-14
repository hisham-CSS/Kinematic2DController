using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    public float jumpHeight = 4;
    public float jumpApexTime = 0.4f;
    public float moveSpeed = 6;

    float velocityXSmoothing;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;


    float jumpForce;
    float gravity;

    Vector3 moveDirection;
    Controller2D controller;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<Controller2D>();
        gravity = -(2 * jumpHeight) / Mathf.Pow(jumpApexTime, 2);
        jumpForce = Mathf.Abs(gravity) * jumpApexTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (controller.collisions.above || controller.collisions.below)
        {
            moveDirection.y = 0;
        }
        
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (controller.collisions.below && Input.GetKeyDown(KeyCode.Space))
        {
            moveDirection.y = jumpForce;
        }

        float targetVelocityX = input.x * moveSpeed;

        moveDirection.x = Mathf.SmoothDamp(moveDirection.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);

        moveDirection.y += gravity * Time.deltaTime;

        controller.Move(moveDirection * Time.deltaTime);
    }
}
