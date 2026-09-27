using System.Collections.Generic;

public class UsedLetters
{

    private readonly HashSet<char> letters = new HashSet<char>();


    public int Count
    {
        get { return letters.Count; }
    }


    public bool Contains(char letter)
    {
        return letters.Contains(Normalize(letter));
    }


    public bool TryAdd(char letter)
    {
        return letters.Add(Normalize(letter));
    }


    public void Clear()
    {
        letters.Clear();
    }


    static char Normalize(char letter)
    {
        return char.ToUpperInvariant(letter);
    }

}
