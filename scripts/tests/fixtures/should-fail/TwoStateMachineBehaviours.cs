using UnityEngine;

// This file should FAIL: contains two StateMachineBehaviour classes
public class TwoStateMachineBehavioursA : StateMachineBehaviour
{
    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex
    ) { }
}

public class TwoStateMachineBehavioursB : StateMachineBehaviour
{
    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex
    ) { }
}
