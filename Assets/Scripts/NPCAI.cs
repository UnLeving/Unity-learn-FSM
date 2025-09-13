using System;
using UnityEngine;
using UnityEngine.AI;

public class NPCAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    public Transform player;
    public State currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        currentState = new IdleState(gameObject, agent, animator, player);
    }

    private void Update()
    {
        currentState = currentState.Process();
    }
}