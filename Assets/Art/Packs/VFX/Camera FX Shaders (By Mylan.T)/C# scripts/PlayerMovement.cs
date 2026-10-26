using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace playerControl
{
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 5.0f;
        public float jumpSpeed = 8.0f;
        public float gravity = 20.0f;

        private CharacterController controller;
        private Vector3 moveDirection = Vector3.zero;

        private float rotationX = 0f;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            locked = false;

        }

        private bool locked;
        void Update()
        {
            if (controller.isGrounded)
            {
                moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
                moveDirection = transform.TransformDirection(moveDirection);
                moveDirection *= speed;


            }
            else
            {
                moveDirection = new Vector3(Input.GetAxis("Horizontal"), moveDirection.y, Input.GetAxis("Vertical"));
                moveDirection = transform.TransformDirection(moveDirection);
                moveDirection.x *= speed;
                moveDirection.z *= speed;
            }

            moveDirection.y -= gravity * Time.deltaTime;
            controller.Move(moveDirection * Time.deltaTime);

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            rotationX -= mouseY * sensetivity;
            rotationX = Mathf.Clamp(rotationX, -30f, 30f);
            if (!locked)
            {
                transform.Rotate(0, mouseX * sensetivity, 0);
                CameraObj.transform.localEulerAngles = new Vector3(rotationX, 0, 0);
            }

            if (Input.GetKeyDown(KeyCode.CapsLock)) locked = !locked;

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                ThrowSphere();
            }
        }
        public GameObject spherePrefab;
        public float throwForce = 10f;

        void ThrowSphere()
        {
            GameObject sphere = Instantiate(spherePrefab, CameraObj.transform.position + CameraObj.transform.forward, Quaternion.identity);

            Rigidbody rb = sphere.GetComponent<Rigidbody>();

            rb.AddForce(CameraObj.transform.forward * throwForce, ForceMode.Impulse);
        }

        public float sensetivity;
        public Transform CameraObj;
    }

}

