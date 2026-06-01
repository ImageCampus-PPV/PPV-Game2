using Assets.Scripts;
using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using UnityEngine;

public class Main : MonoBehaviour
{
    MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    TurnManager TurnManager => ServiceProvider.Instance.GetService<TurnManager>();

    [SerializeField] private APWalletConfiguration _APWalletConfiguration;

    void Awake()
    {
        ServiceProvider.Instance.AddService<EventBus>(new EventBus());
        ServiceProvider.Instance.AddService<MapGrid>(new MapGrid());
        ServiceProvider.Instance.AddService<PathFinding>(new PathFinding());
        ServiceProvider.Instance.AddService<APWallet>(new APWallet(_APWalletConfiguration));
        ServiceProvider.Instance.AddService<EntityRegistry>(new EntityRegistry());
        ServiceProvider.Instance.AddService<TurnManager>(new TurnManager());

        MapGrid.Init();
        APWallet.Init();
        EntityRegistry.Init();
    }

    private void Update()
    {
        TurnManager.Tick();
    }

    private void OnApplicationQuit()
    {
        ServiceProvider.Instance.ClearAllServices();
    }
}
