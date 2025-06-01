using System.Collections.Generic;
using UnityEngine;

public enum NodeType { Base, Mine, Waypoint }

[System.Serializable]
public class NodeConnection
{
    public Node node;
    public float distance;
}

public class Node : MonoBehaviour
{
    public NodeType nodeType;
    [Tooltip("For Bases: resource multiplier\nFor Mines: mining time multiplier")]
    public float multiplier = 1f;

    [SerializeField] private List<NodeConnection> connections = new List<NodeConnection>();

    [HideInInspector] public Dictionary<Node, float> neighborDistances = new Dictionary<Node, float>();
    [HideInInspector] public bool isActive = true;

    private List<float> lastDistances = new List<float>();

    private void Start()
    {
        InitializeNeighborDistances();
        CacheCurrentDistances();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            CheckForDistanceChanges();
        }
    }

    private void CacheCurrentDistances()
    {
        lastDistances.Clear();
        foreach (var conn in connections)
        {
            lastDistances.Add(conn.distance);
        }
    }

    private void CheckForDistanceChanges()
    {
        bool changed = false;

        if (connections.Count != lastDistances.Count)
        {
            changed = true;
        }
        else
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (!Mathf.Approximately(connections[i].distance, lastDistances[i]))
                {
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
        {
            InitializeNeighborDistances();
            CacheCurrentDistances();

            GraphController graphController = FindObjectOfType<GraphController>();
            if (graphController != null)
            {
                graphController.OnNodeDistancesChanged(this);
            }
        }
    }

    public void InitializeNeighborDistances()
    {
        neighborDistances.Clear();
        foreach (var conn in connections)
        {
            if (conn.node != null)
            {
                neighborDistances[conn.node] = conn.distance;
            }
        }
    }

    private void OnDrawGizmos()
    {
        switch (nodeType)
        {
            case NodeType.Base:
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
                break;
            case NodeType.Mine:
                Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.8f);
                break;
            case NodeType.Waypoint:
                Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                break;
        }

        Gizmos.DrawSphere(transform.position, 0.5f);

        Gizmos.color = Color.green;
        foreach (var conn in connections)
        {
            if (conn.node != null)
            {
                Gizmos.DrawLine(transform.position, conn.node.transform.position);
            }
        }
    }
}