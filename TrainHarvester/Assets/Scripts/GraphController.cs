using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GraphController : MonoBehaviour
{
    [Header("References")]
    public GameObject trainPrefab;
    public Text resourcesText;
    public List<Node> nodes = new List<Node>();

    [Header("Simulation Settings")]
    public int initialTrains = 3;
    public float simulationSpeed = 1f;

    private List<Train> trains = new List<Train>();
    private int totalResources;

    private Dictionary<(Node, Node), float> distanceMap = new Dictionary<(Node, Node), float>();
    private Dictionary<(Node, Node), Node> nextNodeMap = new Dictionary<(Node, Node), Node>();
    private Dictionary<(Node, Node), List<Train>> trainsOnEdges = new Dictionary<(Node, Node), List<Train>>();

    private void Start()
    {
        foreach (Node node in nodes)
        {
            node.InitializeNeighborDistances();
        }

        Time.timeScale = simulationSpeed;
        InitializePathfinding();
        SpawnTrains();
        UpdateUI();
    }

    public void OnNodeDistancesChanged(Node changedNode)
    {
        InitializePathfinding();

        foreach (Train train in trains)
        {
            if (train.IsMoving && (train.CurrentNode == changedNode || train.TargetNode == changedNode))
            {
                train.UpdateJourney();
            }
        }
    }

    private void InitializePathfinding()
    {
        distanceMap = new Dictionary<(Node, Node), float>();
        nextNodeMap = new Dictionary<(Node, Node), Node>();

        foreach (Node node in nodes)
        {
            foreach (Node other in nodes)
            {
                var key = (node, other);

                if (node == other)
                {
                    distanceMap[key] = 0f;
                    nextNodeMap[key] = null;
                }
                else if (node.neighborDistances.TryGetValue(other, out float distance))
                {
                    distanceMap[key] = distance;
                    nextNodeMap[key] = other;
                }
                else
                {
                    distanceMap[key] = float.MaxValue;
                    nextNodeMap[key] = null;
                }
            }
        }

        foreach (Node k in nodes)
        {
            foreach (Node i in nodes)
            {
                if (distanceMap[(i, k)] == float.MaxValue) continue;

                foreach (Node j in nodes)
                {
                    if (distanceMap[(k, j)] == float.MaxValue) continue;

                    float ik = distanceMap[(i, k)];
                    float kj = distanceMap[(k, j)];
                    float ij = distanceMap[(i, j)];

                    if (ik + kj < ij)
                    {
                        distanceMap[(i, j)] = ik + kj;
                        nextNodeMap[(i, j)] = nextNodeMap[(i, k)];
                    }
                }
            }
        }
    }

    private void SpawnTrains()
    {
        for (int i = 0; i < initialTrains; i++)
        {
            Node spawnNode = nodes[Random.Range(0, nodes.Count)];
            GameObject trainObj = Instantiate(trainPrefab, spawnNode.transform.position, Quaternion.identity);
            Train train = trainObj.GetComponent<Train>();

            switch (i)
            {
                case 0:
                    train.speed = 200f;
                    train.baseMiningTime = 20f;
                    break;
                case 1:
                    train.speed = 5f;
                    train.baseMiningTime = 1f;
                    break;
                default:
                    train.speed = 80f;
                    train.baseMiningTime = 10f;
                    break;
            }

            train.Initialize(spawnNode, this);
            trains.Add(train);
        }
    }

    public Node FindNearestNodeOfType(Node from, NodeType type)
    {
        Node nearest = null;
        float minDistance = float.MaxValue;
        foreach (Node node in nodes)
        {
            if (!node.isActive || node.nodeType != type) continue;

            float distance = distanceMap[(from, node)];
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = node;
            }
        }
        return nearest;
    }

    public Node GetNextNode(Node from, Node to)
    {
        if (from == to) return null;
        return nextNodeMap.TryGetValue((from, to), out Node next) ? next : null;
    }

    public void AddResources(float amount)
    {
        totalResources += Mathf.RoundToInt(amount);
        UpdateUI();
    }

    private void UpdateUI()
    {
        resourcesText.text = $"Total: {totalResources}";
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        foreach (Train train in trains)
        {
            if (train.hasResource)
            {
                Gizmos.color = Color.green;
            }
            else if (train.isMining)
            {
                Gizmos.color = Color.yellow;
            }
            else
            {
                Gizmos.color = Color.white;
            }

            Gizmos.DrawSphere(train.transform.position, 0.3f);
        }
    }
    public void RegisterTrainOnEdge(Train train, Node from, Node to)
    {
        var edge = (from, to);
        if (!trainsOnEdges.ContainsKey(edge))
        {
            trainsOnEdges[edge] = new List<Train>();
        }
        trainsOnEdges[edge].Add(train);
    }

    public void UnregisterTrainFromEdge(Train train, Node from, Node to)
    {
        var edge = (from, to);
        if (trainsOnEdges.ContainsKey(edge))
        {
            trainsOnEdges[edge].Remove(train);
        }
    }
}