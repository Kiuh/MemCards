using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct TableConfig
{
    public Vector2Int MatrixSize;
    public Vector2 Shift;
    public Transform StartPoint;
}

public class Table : NetworkBehaviour
{
    private TableConfig tableConfig;
    private readonly List<Vector3> cardsPositions = new();
    public List<Vector3> CardsPositions => cardsPositions;

    public int SlotsCount => tableConfig.MatrixSize.x * tableConfig.MatrixSize.y;

    public void SetInitInfo(TableConfig initInfo)
    {
        tableConfig = initInfo;
    }

    public void FillCardsPositions()
    {
        if (tableConfig.MatrixSize.x * tableConfig.MatrixSize.y % 2 != 0)
        {
            Debug.LogError("Require even matrix!");
        }
        cardsPositions.Clear();
        Vector3 creatingPosition = tableConfig.StartPoint.position;
        for (int i = 0; i < tableConfig.MatrixSize.x; i++)
        {
            for (int j = 0; j < tableConfig.MatrixSize.y; j++)
            {
                cardsPositions.Add(creatingPosition);
                creatingPosition.z += tableConfig.Shift.y;
            }
            creatingPosition.x += tableConfig.Shift.x;
            creatingPosition.z = tableConfig.StartPoint.transform.position.z;
        }
    }
}
