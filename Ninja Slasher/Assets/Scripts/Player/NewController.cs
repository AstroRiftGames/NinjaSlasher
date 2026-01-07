using Managers;
using System.Linq;
using UnityEngine;

public enum NinjaStates
{
    Idle,
    Dash,
    Grab,
    KO
}

public class NewController : MonoBehaviour
{
    public View View => _view;
    [SerializeField] View _view;
    public Model Model => _model;
    [SerializeField] Model _model;

    private bool _isKO = false;
    private bool _isDashing = false;
    private string[] colMatrix = { "Obstacle", "Scenario", "Platform"};


    #region FSM and Behavior Tree Setup
    //private FSM<NinjaStates> _fsm;
    //private ITreeNode _root;

    //private void InitializeFSM()
    //{
    //    var idleState = new NinjaIdleState<NinjaStates>();
    //    var dashState = new NinjaDashState<NinjaStates>();
    //    var grabState = new NinjaGrabState<NinjaStates>();
    //    var koState = new NinjaKOState<NinjaStates>();

    //    idleState.AddTransition(NinjaStates.Dash, dashState);
    //    idleState.AddTransition(NinjaStates.KO, koState);
    //    dashState.AddTransition(NinjaStates.Grab, grabState);
    //    dashState.AddTransition(NinjaStates.KO, koState);
    //    grabState.AddTransition(NinjaStates.Dash, dashState);
    //    grabState.AddTransition(NinjaStates.KO, koState);
    //    koState.AddTransition(NinjaStates.Idle, idleState);

    //    _fsm = new FSM<NinjaStates>(idleState);
    //}

    //private void InitializeTree()
    //{
    //    ITreeNode idle = new ActionNode(() => { _fsm.Transition(NinjaStates.Idle); });
    //    ITreeNode dash = new ActionNode(() => { _fsm.Transition(NinjaStates.Dash); });
    //    ITreeNode grab = new ActionNode(() => { _fsm.Transition(NinjaStates.Grab); });
    //    ITreeNode ko = new ActionNode(() => { _fsm.Transition(NinjaStates.KO); });

    //    ITreeNode rootQ = new QuestionNode(QKO, ko,
    //                      new QuestionNode(QGrab, grab,
    //                      new QuestionNode(QDash, dash, idle)));
    //    _root = rootQ;
    //}


    //private bool QKO() => false;
    //private bool QGrab() => false;
    //private bool QDash() => false;

    #endregion

    #region MAGIC METHODS
    private void Awake()
    {
        //InitializeFSM();
        //InitializeTree();
    }
    void OnEnable()
    {
        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    private void OnDisable()
    {
        CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
    }

    private void CustomUpdate()
    {
        if(_isKO)
        {
            Debug.Log("Player is KO'd");
        }
        else
        {
            Debug.Log("Player is active");
            CheckInput();
        }
    }
    #endregion

    #region INPUT HANDLING
    private void CheckInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryDash();
        }
    }
    #endregion

    #region MECHANICS
    private void TryDash()
    {
        if(!_isDashing)
        Dash();
    }
    private void Dash()
    {
        Debug.Log("Player Dashes");
        _view.RB.AddForce(GetDirection() * _model.DashForce);
        _isDashing = true;
    }

    private Vector2 GetDirection()
    {
        Vector2 v = Vector2.zero;
        v = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        Debug.Log(v.normalized);
        return v.normalized;
    }

    private void Grab()
    {
        _isDashing = false;
        _view.RB.linearVelocity = Vector2.zero;
    }
    #endregion

    #region COLLISION DETECTION
    private void OnCollisionEnter2D(Collision2D collision)
    {
        string colTag = collision.gameObject.tag;
        if (colMatrix.Contains(colTag))
        {
            Grab();
        }
    }
    #endregion
}