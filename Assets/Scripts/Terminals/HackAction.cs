using System.Collections;

public class HackAction : TurnAction
{
    private readonly Player _player;
    private readonly Terminal _terminal;
    private readonly int _ticksToResolve;

    public HackAction(Player player, Terminal terminal, int ticksToResolve, int apCost) : base(ticksToResolve, apCost)
    {
        _player = player;
        _terminal = terminal;
        _ticksToResolve = ticksToResolve;
    }

    public Terminal Terminal => _terminal;

    public override IEnumerator Execute(Unit unit)
    {
        yield return _player.PlayHackAnimation(_terminal, _ticksToResolve);

        AdvanceTick();
    }
}
