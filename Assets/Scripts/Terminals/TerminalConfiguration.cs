using System;
using UnityEngine;

// Balance por defecto de cada tipo de terminal (AP, ticks y rango).
// Ver "Coste y Duracion del Hackeo" en MEC-02 - referirse a la planilla de
// balance de Hackeo de Terminales para los valores finales:
// https://docs.google.com/spreadsheets/d/18MzORZ__stUr4dxP7f76QjLdmnyHWNhl-u703041BXY
[Serializable]
public struct TerminalBalanceData
{
    public TerminalType type;
    public int apCost;
    public int requiredTicks;
    public int range;
}

[CreateAssetMenu(fileName = "TerminalConfiguration", menuName = "ScriptableObjects/TerminalConfiguration")]
public class TerminalConfiguration : ScriptableObject
{
    [SerializeField] private TerminalBalanceData[] _balancePerType;

    public TerminalBalanceData GetBalance(TerminalType type)
    {
        if (_balancePerType != null)
            foreach (TerminalBalanceData data in _balancePerType)
                if (data.type == type)
                    return data;

        Debug.LogWarning($"[TerminalConfiguration] No hay balance configurado para {type}. Usando valores por defecto (1 AP, 1 tick, rango 1).");
        return new TerminalBalanceData { type = type, apCost = 1, requiredTicks = 1, range = 1 };
    }
}
