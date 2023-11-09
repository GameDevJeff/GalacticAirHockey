using UnityEngine;

public class PuckControls : MonoBehaviour
{
    public Camera camera;
    public LayerMask layerMask;
    public GameObject puck;
    private Rigidbody rigidBody;
    private Vector3 velocity;

    public Vector3 velocityToNet;
    public Vector3 velocityFromNet;
    public bool sentVelocity = false;
    public bool onNet = false;

    private bool paddleGrabed = false;

    public float desiredTime = 10;
    public float lockOnSpeed = 10;
    public float hoverSpeed = 5;
    public float hoverRange = 2;
    private float curTime = 0;
    private float curTime2 = 0;

    public GameObject playareas;
    private bool cpuHitPuck = false;

    public bool player1;
    public bool CPUPlayer;

    //freeze prop
    public bool stopPaddle = false;
    private Vector3 positionPrevious = new();

    //CPU
    private float CPUXMax = 1.5f, CPUXMin = 4.5f;
    private float CPUXUpBounds, CPUXLowBounds;



    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponentInChildren<Rigidbody>();
        if (player1)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), playareas.GetComponentsInChildren<Collider>()[0]);
        }
        else
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), playareas.GetComponentsInChildren<Collider>()[1]);

        }

        velocityToNet = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        CPUXMax = -1.5f;
        CPUXMin = -4f;

        rigidBody.isKinematic = false;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                if (raycastHit.collider.gameObject == gameObject)
                    paddleGrabed = true;
                Debug.Log("Grabbed Paddle");
            }
        }
        else
            paddleGrabed = false;

        rigidBody.isKinematic = false;

    }


    public void DisablePaddle()
    {
        paddleGrabed = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (stopPaddle)
        {
            transform.position = positionPrevious;
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (paddleGrabed && Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask))
        {
            if (onNet)
            {
                velocityToNet = Vector3.Scale(raycastHit.point, (new Vector3(1, 0, 1))) + new Vector3(0, transform.position.y, 0);
                if ((player1 && velocityToNet.x < 0f) || (!player1 && velocityToNet.x > 0f))
                    velocityToNet.x = 0f;
                if (velocityToNet.z > 2.6f)
                    velocityToNet.z = 2.6f;
                if (velocityToNet.z < -2.6f)
                    velocityToNet.z = -2.6f;
                
            }
            else
                velocity = raycastHit.point - transform.position;
        }

        if (onNet)
        {
            //Debug.Log("PC VelocityFromNet: " + velocityFromNet);
            if(sentVelocity)
                transform.position = velocityFromNet;
            //rigidBody.velocity = velocityFromNet;
        }
        else if (!CPUPlayer)
        {
            //transform.position = velocity;
            rigidBody.velocity = velocity * 10000.0f * Time.deltaTime;
        }


        if (CPUPlayer && puck.GetComponent<PuckLogic>().grounded)
        {
            //CPUPassive();
            CPUShifting();

            CPULockOn();

            CPUShoot();
        }

        positionPrevious.Set(transform.position.x, transform.position.y, transform.position.z);
    }
    void CPUPassive()
    {
        curTime += Time.deltaTime;
        float percentageCompleted = curTime / hoverSpeed * 0.25f;
        curTime = percentageCompleted >= 1.0f ? 0.0f : curTime;

        Vector3 backAndForth = new Vector3(0, 0, Mathf.Cos(Time.time * hoverSpeed));
        Vector3 removeZ = new Vector3(1, 1, 0);

        transform.position = Vector3.Lerp(transform.position, Vector3.Scale(transform.position, removeZ) + backAndForth * hoverRange, percentageCompleted);
    }
    void CPUShifting ()
    {
        curTime += Time.deltaTime;
        float percentageCompleted = curTime / hoverSpeed;
        curTime = percentageCompleted >= 1.0f ? 0.0f : curTime;

        //Vector3 forwardAndBack = new Vector3(Mathf.Cos(Time.time * hoverSpeed), 0, 0);
        Vector3 shifting;
        if (puck.transform.position.x > 0 || puck.transform.position.x < CPUXLowBounds)
        {
            shifting = new Vector3(CPUXUpBounds, 0f, 0f);
        }
        else
        {
            shifting = new Vector3(puck.transform.position.x - puck.transform.localScale.x, 0,0);
        }
        Vector3 removeX = new Vector3(0, 1, 1);

        transform.position = Vector3.Lerp(transform.position, Vector3.Scale(transform.position, removeX) + shifting, percentageCompleted);
    }

    void CPULockOn()
    {
        Vector3 addZ = new Vector3(0, 0, puck.transform.position.z);
        Vector3 removeZ = new Vector3(1, 1, 0);

        curTime = Mathf.Abs(transform.position.z - puck.transform.position.z) < 0.1f ? 0.0f : curTime;

        curTime += Time.deltaTime;
        float curProgress = curTime / lockOnSpeed;

        transform.position = Vector3.Lerp(transform.position, Vector3.Scale(transform.position, removeZ) + addZ, curProgress);
    }

    void CPUShoot()
    {
        Vector3 removeY = new Vector3(1, 0, 1);
        Vector3 addY = new Vector3(0, transform.position.y, 0);

        if ((puck.transform.position - transform.position).x < 1.0f && !cpuHitPuck)
        {
            curTime2 += Time.deltaTime;
            float curProgress = curTime / desiredTime * 0.5f;

            transform.position = Vector3.Lerp(transform.position, Vector3.Scale(puck.transform.position, removeY) + addY, curProgress);
        }
        else if ((puck.transform.position - transform.position).x > 1.0f && cpuHitPuck)
        {
            cpuHitPuck = false;
        }

        rigidBody.velocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == puck)
        {
            if (onNet)
                collision.rigidbody.velocity = (collision.transform.position - transform.position) * 20f;
            else if (CPUPlayer)
            {
                collision.rigidbody.velocity = (collision.transform.position - transform.position) * 30f;
                Vector3 opoHitVector = transform.position - collision.transform.position;
                opoHitVector = opoHitVector.normalized;
                rigidBody.AddForce(opoHitVector * 75f, ForceMode.Impulse);
                cpuHitPuck = true;
                curTime2 = 0;
            }
        }
    }

    public void CPUOnWhichSide(bool p1side)
    {
        if (p1side)
        {
            CPUXUpBounds  = CPUXMax;
            CPUXLowBounds = CPUXMin;
        }
        else
        {
            CPUXUpBounds  = CPUXMax * -1f;
            CPUXLowBounds = CPUXMin * -1f;
        }
    }
}
