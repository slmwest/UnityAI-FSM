using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;

public class State
{
    public enum STATE
    {
        IDLE, PATROL, PURSUE, ATTACK, SLEEP
    };

    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    };

    public STATE name; // this won't be defined in the base class, only in inherited classes that build on it!
    protected EVENT stage;
    protected GameObject npc;
    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform player;
    protected State nextState;

    float visDist = 10.0f;
    float visAngle = 30.0f;
    float shootDist = 7.0f;

    // constructor
    public State(GameObject _npc, NavMeshAgent _agent, Animator _anim, Transform _player)
    {
        npc = _npc;
        agent = _agent;
        anim = _anim;
        stage = EVENT.ENTER;
        player = _player;
    }

    // Event stages
    public virtual void Enter() { stage = EVENT.UPDATE; }
    public virtual void Update() { stage = EVENT.UPDATE; }
    public virtual void Exit() { stage = EVENT.EXIT; }

    // progress throguh stages from Enter through Update to Exit
    public State Process()
    {
        if (stage == EVENT.ENTER) Enter();
        if (stage == EVENT.UPDATE) Update();
        if (stage == EVENT.EXIT)
        {
            Exit();
            return nextState; // return next state
        }
        return this; // return current state
    }

    public bool CanSeePlayer()
    {
        // calculate angle using diretion vector and npc forward vector
        Vector3 direction = player.position - npc.transform.position;
        float angle = Vector3.Angle(direction, npc.transform.forward);

        if (direction.magnitude < visDist && angle < visAngle) // 30degree in either direction gives 60degree arc
        {
            return true;
        }
        return false;
    }

    public bool CanAttackPlayer()
    {
        // calculate angle using diretion vector and npc forward vector
        Vector3 direction = player.position - npc.transform.position;
        //float angle = Vector3.Angle(direction, npc.transform.forward);
        if (direction.magnitude < shootDist) // && angle < visAngle) // 30degree in either direction gives 60degree arc
        {
            return true;
        }
        return false;
    }

}

// better practice to do this in another file in a production project!
public class Idle : State
{
    // constructor, where base keyword used to call constructor of the base class. We pass the Idle inputs to the base class constructor.
    public Idle(GameObject _npc, NavMeshAgent _agent, Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.IDLE;
    }

    public override void Enter()
    {
        agent.speed = 0; //navMesh property
        agent.isStopped = true; //navMesh property
        anim.SetTrigger("isIdle");
        base.Enter();
    }

    public override void Update()
    {
        // probabilistic transitions from this state or remain in this state
        if (Random.Range(0, 100) < 2)
        {
            nextState = new Patrol(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
        //base.Update(); // this prevents any exit transition from ever happening as it over-writes stage to be event.update again!
    }

    public override void Exit()
    {
        anim.ResetTrigger("isIdle"); // clean up queue of triggers, reduce risk of buggy anims. see McAdams system
        base.Exit();
    }
}

public class Patrol : State
{
    int currentIndex = -1;

    // constructor, where base keyword used to call constructor of the base class. We pass the Patrol inputs to the base class constructor.
    public Patrol(GameObject _npc, NavMeshAgent _agent, Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.PATROL;
        agent.speed = 2; //navMesh property
        agent.isStopped = false; //navMesh property
    }

    public override void Enter()
    {
        currentIndex = 0;
        anim.SetTrigger("isWalking");
        //agent.SetDestination(GameEnvironment.Singleton.Checkpoints[currentIndex].transform.position);
        base.Enter();
    }

    public override void Update()
    {
        
        //if (CanSeePlayer())
        //{
        //    nextState = new Pursue(npc, agent, anim, player);
        //    stage = EVENT.EXIT;
        //}
        
        if (agent.remainingDistance < 1)
        {
            if (currentIndex >= GameEnvironment.Singleton.Checkpoints.Count - 1)
            {
                currentIndex = 0;
            } else
            {
                currentIndex++;
            }
            agent.SetDestination(GameEnvironment.Singleton.Checkpoints[currentIndex].transform.position);
        }

        // probabilistic transitions from this state or remain in this state
        if (Random.Range(0, 1000) < -1) 
        {
            nextState = new Idle(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }

        //base.Update(); // this prevents any exit transition from ever happening as it over-writes stage to be event.update again!
    }

    public override void Exit()
    {
        anim.ResetTrigger("isWalking"); // clean up queue of triggers, reduce risk of buggy anims. see McAdams system
        base.Exit();
    }
}

public class Pursue : State
{
    // constructor, where base keyword used to call constructor of the base class. We pass the Patrol inputs to the base class constructor.
    public Pursue(GameObject _npc, NavMeshAgent _agent, Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.PURSUE;
        agent.speed = 5; //navMesh property. now running rather than walking!
        agent.isStopped = false; //navMesh property
    }

    public override void Enter()
    {
        anim.SetTrigger("isRunning");
        base.Enter();
    }

    public override void Update()
    {
        agent.SetDestination(player.transform.position);
        if (agent.hasPath) // wait long enough for SetDestination to create a path. E.g. it could take at least one Update / Frame to get there!
        {
            if (CanAttackPlayer())
            {
                nextState = new Attack(npc, agent, anim, player);
                stage = EVENT.EXIT;
            }
            else if (!CanSeePlayer())
            {
                nextState = new Patrol(npc, agent, anim, player);
                stage = EVENT.EXIT;
            }
        }
    }

    public override void Exit()
    {
        anim.ResetTrigger("isRunning");
        base.Exit();
    }
}


public class Attack : State
{
    // should define these elsewhere!
    float rotationSpeed = 2.0f;
    AudioSource shoot; 

    // constructor, where base keyword used to call constructor of the base class. We pass the Patrol inputs to the base class constructor.
    public Attack(GameObject _npc, NavMeshAgent _agent, Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.ATTACK;
        shoot = _npc.GetComponent<AudioSource>(); // should define this elsewhere!
    }

    public override void Enter()
    {
        anim.SetTrigger("isShooting");
        agent.isStopped = true;
        shoot.Play();
        base.Enter();
    }

    public override void Update()
    {
        // TO DO
        base.Update();
    }

    public override void Exit()
    {
        anim.ResetTrigger("isShooting");
        agent.isStopped = false;
        base.Exit();
    }
}