using UnityEngine;

public class PuckLogic : MonoBehaviour
{
    private Rigidbody rigidbody;
    private Collider AHTFloorCollider;
    public GameObject AHTableFloor;
    public GameObject playareas;
    private Vector3 prevPos = new();
    public Vector3 positionPrevious { get { return new Vector3(prevPos.x, prevPos.y, prevPos.z); } }
    public bool grounded = false;
    public bool stopPuck = false;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        AHTFloorCollider = AHTableFloor.GetComponent<Collider>();

        foreach (Collider collider in playareas.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(collider, GetComponent<Collider>());
        }
        rigidbody.isKinematic = false;
    }

    private void FixedUpdate()
    {
        if (stopPuck)
        {
            transform.position = prevPos;
            return;
        }

        prevPos.Set(transform.position.x, transform.position.y, transform.position.z);
        rigidbody.isKinematic = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == AHTFloorCollider)
        {
            rigidbody.constraints = rigidbody.constraints | RigidbodyConstraints.FreezePositionY;
            grounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider == AHTFloorCollider)
        {
            rigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
            grounded = false;
        }
    }
}
