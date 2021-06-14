using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour
{
    public LayerMask collisionMask;
    public CollisionInfo collisions;

    new BoxCollider2D collider;
    const float insetWidth = 0.015f;

    public int horizonatalRayCount = 4;
    public int verticalRayCount = 4;

    float maxClimbAngle = 80;
    float maxDescendAngle = 80;


    float horizonatalRaySpacing;
    float verticalRaySpacing;

    RaycastOrigins originPoints;


    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();
    }
    void UpdateRaycastOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(insetWidth * -2);

        originPoints.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        originPoints.bottomRight = new Vector2(bounds.max.x, bounds.min.y);

        originPoints.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        originPoints.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }
    void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(insetWidth * -2);

        horizonatalRayCount = Mathf.Clamp(horizonatalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizonatalRaySpacing = bounds.size.y / (horizonatalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public void Move(Vector3 moveDirection)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.oldMoveDirection = moveDirection;

        //collision detection

        if (moveDirection.y < 0)
            DescendSlope(ref moveDirection);

        if (moveDirection.x != 0)
            HorizontalCollisions(ref moveDirection);

        if (moveDirection.y != 0)
            VerticalCollisions(ref moveDirection);


        //movement
        transform.Translate(moveDirection);
    }

    

    

    void VerticalCollisions(ref Vector3 moveDirection)
    {
        float directionY = Mathf.Sign(moveDirection.y);
        float directionX = Mathf.Sign(moveDirection.x);
        float rayLength = Mathf.Abs(moveDirection.y) + insetWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? originPoints.bottomLeft : originPoints.topLeft;

            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveDirection.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                moveDirection.y = (hit.distance - insetWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    moveDirection.x = moveDirection.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * directionX;
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }

        }

        if (collisions.climbingSlope)
        {
            rayLength = Mathf.Abs(moveDirection.x) + insetWidth;
            Vector2 rayOrigin = ((directionX == -1) ? originPoints.bottomLeft : originPoints.bottomRight) + Vector2.up * moveDirection.y;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    moveDirection.x = (hit.distance - insetWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void HorizontalCollisions(ref Vector3 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        float rayLength = Mathf.Abs(moveDirection.x) + insetWidth;

        for (int i = 0; i < horizonatalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? originPoints.bottomLeft : originPoints.bottomRight;

            rayOrigin += Vector2.up * (horizonatalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                
                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    float distanceToSlopeStart = 0;
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveDirection = collisions.oldMoveDirection;
                    }
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - insetWidth;
                        moveDirection.x -= distanceToSlopeStart * directionX;
                    }

                    ClimbSlope(ref moveDirection, slopeAngle, directionX);
                    moveDirection.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    moveDirection.x = (hit.distance - insetWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        moveDirection.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveDirection.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;

                }
            }

        }
    }

    private void ClimbSlope(ref Vector3 moveDirection, float slopeAngle, float directionX)
    {
        //move distance up the slope and slope angle can be used to find out the length and height of the slope that we need to reach
        //SOH CAH TOA
        //opp = hyp * sin(theta)
        //adj = hyp * cos(theta)

        float moveDistance = Mathf.Abs(moveDirection.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (moveDirection.y <= climbVelocityY)
        {
            //here is where you can implement a smooth slowdown up the incline
            moveDirection.y = climbVelocityY;

            moveDirection.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * directionX;
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }


    }

    void DescendSlope(ref Vector3 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        Vector2 rayOrigin = ((directionX == -1) ? originPoints.bottomRight : originPoints.bottomLeft);

        //optimizations for raycast distance can be made here
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                //Lets move down the slope
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - insetWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveDirection.x))
                    {
                        float moveDistance = Mathf.Abs(moveDirection.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

                        moveDirection.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * directionX;
                        moveDirection.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                        Debug.Log(directionX);
                    }
                }
            }
        }
    }

    struct RaycastOrigins
    {
        public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }    

    public struct CollisionInfo
    {
        public bool above, below, left, right;

        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld;

        public Vector3 oldMoveDirection;

        public void Reset()
        {
            above = below = left = right = false;
            descendingSlope = climbingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
