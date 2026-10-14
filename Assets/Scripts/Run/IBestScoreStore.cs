/// <summary>
/// Storage seam for the local personal best. The game uses
/// <see cref="PlayerPrefsBestScoreStore"/>; tests use an in-memory fake.
/// </summary>
public interface IBestScoreStore
{
    PersonalBest Load();
    void Save(PersonalBest best);
}
