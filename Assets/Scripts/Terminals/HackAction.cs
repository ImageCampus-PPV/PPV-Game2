using ImageCampus.ToolBox.Services;
using System.Collections;

public class HackAction : TurnAction
{
    private readonly Player _player;
    private readonly Terminal _terminal;
    private readonly int _ticksToResolve;

    private HackSystem HackSystem => ServiceProvider.Instance.GetService<HackSystem>();

    public HackAction(Player player, Terminal terminal, int ticksToResolve, int apCost) : base(ticksToResolve, apCost)
    {
        _player = player;
        _terminal = terminal;
        _ticksToResolve = ticksToResolve;
    }

    public Terminal Terminal => _terminal;

    public override IEnumerator Execute(Unit unit)
    {
        HackSystem.ResolvePlannedHack(_player, _terminal, _ticksToResolve);

        yield return null;

        AdvanceTick();
    }
}
