using UnityEngine;

//runs the current "phase"
public class GameFlowController : MonoBehaviour
{
    private void HandleSequenceCompleted()
    {
        Debug.Log("GameFlowController: spawn sequence completed");
        // Continue tutorial / level progression here
    }
}