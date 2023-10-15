using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct TableConfig : INetworkSerializable
{
    public Vector2Int MatrixSize;
    public Vector2 Shift;
    public Transform StartPoint;
    public Vector3 StartPointVector;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref MatrixSize);
        serializer.SerializeValue(ref Shift);
        serializer.SerializeValue(ref StartPointVector);
    }
}

public class Table : NetworkBehaviour
{
    [SerializeField]
    private NetworkVariable<TableConfig> tableConfig = new();
    public TableConfig TableConfig
    {
        get => tableConfig.Value;
        set => tableConfig.Value = value;
    }
    private readonly List<Vector3> cardsPositions = new();
    public List<Vector3> CardsPositions => cardsPositions;

    public void FillCardsPositions()
    {
        _ = StartCoroutine(WaitForData());
    }

    private IEnumerator WaitForData()
    {
        while (TableConfig.MatrixSize == Vector2.zero)
        {
            yield return new WaitForEndOfFrame();
        }
        if (TableConfig.MatrixSize.x * TableConfig.MatrixSize.y % 2 != 0)
        {
            Debug.LogError("Require even matrix!");
        }
        cardsPositions.Clear();
        Vector3 creatingPosition = TableConfig.StartPointVector;
        for (int i = 0; i < TableConfig.MatrixSize.x; i++)
        {
            for (int j = 0; j < TableConfig.MatrixSize.y; j++)
            {
                cardsPositions.Add(creatingPosition);
                creatingPosition.z += TableConfig.Shift.y;
            }
            creatingPosition.x += TableConfig.Shift.x;
            creatingPosition.z = TableConfig.StartPointVector.z;
        }
    }
}
