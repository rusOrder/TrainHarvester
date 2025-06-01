using UnityEngine;

public class Train : MonoBehaviour
{
    [Header("Parameters")]
    public float speed = 10f;
    public float baseMiningTime = 5f;

    [Header("State")]
    public bool hasResource = false;
    public bool isMining = false;

    public Node CurrentNode { get; private set; }
    public Node TargetNode { get; private set; }
    public bool IsMoving => TargetNode != null;

    private float journeyLength;
    private float expectedJourneyTime;
    private float startTime;

    private GraphController graphController;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    public void Initialize(Node startNode, GraphController controller)
    {
        CurrentNode = startNode;
        graphController = controller;
        transform.position = CurrentNode.transform.position;
        DecideNextAction();
    }

    private void Update()
    {
        if (IsMoving)
        {
            float elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / expectedJourneyTime);
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            if (progress >= 1f) ArriveAtNode(TargetNode);
        }
    }

    private void StartMoving(Node target)
    {
        if (IsMoving) graphController.UnregisterTrainFromEdge(this, CurrentNode, TargetNode);

        TargetNode = target;
        startPosition = transform.position;
        targetPosition = TargetNode.transform.position;

        if (CurrentNode.neighborDistances.TryGetValue(TargetNode, out float distance))
        {
            journeyLength = distance;
        }
        else
        {
            journeyLength = Vector3.Distance(startPosition, targetPosition);
        }
        expectedJourneyTime = journeyLength / speed;
        startTime = Time.time;
        graphController.RegisterTrainOnEdge(this, CurrentNode, TargetNode);
    }

    private void ArriveAtNode(Node node)
    {
        graphController.UnregisterTrainFromEdge(this, CurrentNode, TargetNode);
        CurrentNode = node;
        TargetNode = null;
        transform.position = CurrentNode.transform.position;
        DecideNextAction();
    }

    public void UpdateJourney()
    {
        if (!IsMoving) return;

        float currentProgress = Mathf.Clamp01((Time.time - startTime) / expectedJourneyTime);
        Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, currentProgress);

        if (CurrentNode.neighborDistances.TryGetValue(TargetNode, out float newDistance))
        {
            journeyLength = newDistance;
        }
        else
        {
            journeyLength = Vector3.Distance(startPosition, targetPosition);
        }
        float remainingDistance = journeyLength * (1 - currentProgress);
        expectedJourneyTime = remainingDistance / speed;
        startTime = Time.time;
        transform.position = currentPosition;
        startPosition = currentPosition;
    }

    private void StartMining()
    {
        isMining = true;
        float miningTime = baseMiningTime * CurrentNode.multiplier;
        Invoke("FinishMining", miningTime);
    }
    
    private void FinishMining()
    {
        isMining = false;
        hasResource = true;
        DecideNextAction();
    }

    private void DeliverResource()
    {
        graphController.AddResources(CurrentNode.multiplier);
        hasResource = false;
        DecideNextAction();
    }

    private void DecideNextAction()
    {
        if (isMining) return;

        if (hasResource)
        {
            if (CurrentNode.nodeType == NodeType.Base)
            {
                DeliverResource();
            }
            else
            {
                Node targetBase = graphController.FindNearestNodeOfType(CurrentNode, NodeType.Base);
                if (targetBase != null)
                {
                    Node nextNode = graphController.GetNextNode(CurrentNode, targetBase);
                    if (nextNode != null) StartMoving(nextNode);
                }
            }
        }
        else
        {
            if (CurrentNode.nodeType == NodeType.Mine)
            {
                StartMining();
            }
            else
            {
                Node targetMine = graphController.FindNearestNodeOfType(CurrentNode, NodeType.Mine);
                if (targetMine != null)
                {
                    Node nextNode = graphController.GetNextNode(CurrentNode, targetMine);
                    if (nextNode != null) StartMoving(nextNode);
                }
            }
        }
    }
}