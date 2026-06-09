public interface IAbility
{
    string Name { get; }

    int APCost { get; }

    int Range { get; }

    bool CanExecute(Player player, Cell targetCell);

    void Execute(Player player, Cell targetCell);
}