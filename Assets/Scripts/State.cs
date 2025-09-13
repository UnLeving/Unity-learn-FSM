using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class State
{
    public enum STATE
    {
        IDLE,
        PATROL,
        PURSUE,
        ATTACK,
        SLEEP,
        RUNAWAY
    }

    public enum EVENT
    {
        ENTER,
        UPDATE,
        EXIT
    }

    public STATE name;
    protected EVENT stage;
    protected GameObject npc;
    protected Animator animator;
    protected Transform player;
    protected State nextState;
    protected NavMeshAgent agent;

    private float visDist = 10f;
    private float visAngle = 30f;
    private float shootDist = 7f;

    public State(GameObject npc, NavMeshAgent agent, Animator animator, Transform player)
    {
        this.npc = npc;
        this.animator = animator;
        this.player = player;
        this.stage = EVENT.ENTER;
        this.agent = agent;
    }

    public virtual void Enter()
    {
        stage = EVENT.UPDATE;
    }

    public virtual void Update()
    {
        stage = EVENT.UPDATE;
    }

    public virtual void Exit()
    {
        stage = EVENT.EXIT;
    }

    public State Process()
    {
        if (stage == EVENT.ENTER) Enter();
        if (stage == EVENT.UPDATE) Update();
        if (stage == EVENT.EXIT)
        {
            Exit();

            return nextState;
        }

        return this;
    }

    public bool CanSeePlayer()
    {
        var direction = player.position - npc.transform.position;
        var angle = Vector3.Angle(direction, npc.transform.forward);

        if (direction.magnitude < visDist && angle < visAngle)
        {
            return true;
        }

        return false;
    }

    public bool CanAttackPlayer()
    {
        var direction = player.position - npc.transform.position;

        if (direction.magnitude < shootDist)
        {
            return true;
        }

        return false;
    }
    
    public bool IsPlayerBehind()
    {
        var direction = npc.transform.position - player.position;
        var angle = Vector3.Angle(direction, npc.transform.forward);

        if (direction.magnitude < 2 && angle < 30)
        {
            return true;
        }
        
        return false;
    }
}

public class IdleState : State
{
    private static readonly int IsIdle = Animator.StringToHash("isIdle");

    public IdleState(GameObject npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player)
    {
        name = STATE.IDLE;
    }

    public override void Enter()
    {
        animator.SetTrigger(IsIdle);

        base.Enter();
    }

    public override void Update()
    {
        if (CanSeePlayer())
        {
            nextState = new PursueState(npc, agent, animator, player);
            
            stage = EVENT.EXIT;
        }
        else if (Random.Range(0, 100) < 10)
        {
            nextState = new PatrolState(npc, agent, animator, player);

            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        animator.ResetTrigger(IsIdle);

        base.Exit();
    }
}

public class PatrolState : State
{
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private int patrolIndex = -1;
    private List<GameObject> InstanceCheckpoints => GameEnvironment.Instance.Checkpoints;

    public PatrolState(GameObject gameObject, NavMeshAgent agent, Animator animator, Transform transform)
        : base(gameObject, agent, animator, transform)
    {
        name = STATE.PATROL;
        agent.speed = 2f;
        agent.isStopped = false;
    }

    public override void Enter()
    {
        float distance = Single.MaxValue;

        foreach (var checkpoint in InstanceCheckpoints)
        {
            var dist = Vector3.Distance(npc.transform.position, checkpoint.transform.position);
            
            if (dist < distance)
            {
                distance = dist;
             
                patrolIndex = InstanceCheckpoints.IndexOf(checkpoint) - 1; // in update we do ++
            }
            
        }

        animator.SetTrigger(IsWalking);

        base.Enter();
    }

    public override void Update()
    {
        if (agent.remainingDistance < 1)
        {
            if (patrolIndex >= InstanceCheckpoints.Count - 1)
            {
                patrolIndex = 0;
            }
            else
            {
                patrolIndex++;
            }

            agent.SetDestination(InstanceCheckpoints[patrolIndex].transform.position);
        }
        
        if (CanSeePlayer())
        {
            nextState = new PursueState(npc, agent, animator, player);
            
            stage = EVENT.EXIT;
        }
        else if (IsPlayerBehind())
        {
            nextState = new RunawayState(npc, agent, animator, player);
            
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        animator.ResetTrigger(IsWalking);

        base.Exit();
    }
}

public class RunawayState : State
{
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private GameObject safeLocation;
    
    public RunawayState(GameObject gameObject, NavMeshAgent navMeshAgent, Animator animator1, Transform transform)
        : base(gameObject, navMeshAgent, animator1, transform)
    {
        name = STATE.RUNAWAY;
        
        safeLocation = GameObject.FindGameObjectWithTag("Safe");
    }

    public override void Enter()
    {
        animator.SetTrigger(IsRunning);
        agent.isStopped = false;
        agent.speed = 6f;
        agent.SetDestination(safeLocation.transform.position);
        
        base.Enter();
    }
    
    public override void Update()
    {
        if (agent.remainingDistance < 1)
        {
            nextState = new IdleState(npc, agent, animator, player);
            
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        animator.ResetTrigger(IsRunning);
        
        base.Exit();
    }
}

public class PursueState : State
{
    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    public PursueState(GameObject gameObject, NavMeshAgent agent, Animator animator, Transform transform)
        : base(gameObject, agent, animator, transform)
    {
        name = STATE.PURSUE;
        agent.speed = 5f;
        agent.isStopped = false;
    }

    public override void Enter()
    {
        animator.SetTrigger(IsRunning);

        base.Enter();
    }

    public override void Update()
    {
        agent.SetDestination(player.position);

        if (agent.hasPath)
        {
            if (CanAttackPlayer())
            {
                nextState = new AttackState(npc, agent, animator, player);

                stage = EVENT.EXIT;
            }
            else if (CanSeePlayer() == false)
            {
                nextState = new PatrolState(npc, agent, animator, player);

                stage = EVENT.EXIT;
            }
        }
    }

    public override void Exit()
    {
        animator.ResetTrigger(IsRunning);

        base.Exit();
    }
}

public class AttackState : State
{
    private static readonly int IsShooting = Animator.StringToHash("isShooting");

    private float rotateSpeed = 2f;
    private AudioSource audioSource;

    public AttackState(GameObject npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player)
    {
        name = STATE.ATTACK;

        audioSource = npc.GetComponent<AudioSource>();
    }

    public override void Enter()
    {
        animator.SetTrigger(IsShooting);

        agent.isStopped = true;

        audioSource.Play();

        base.Enter();
    }

    public override void Update()
    {
        var dir = player.position - npc.transform.position;
        var angle = Vector3.Angle(dir, npc.transform.forward);
        dir.y = 0;

        npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(dir),
            rotateSpeed * Time.deltaTime);

        if (CanAttackPlayer() == false)
        {
            nextState = new IdleState(npc, agent, animator, player);

            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        animator.ResetTrigger(IsShooting);

        base.Exit();
    }
}