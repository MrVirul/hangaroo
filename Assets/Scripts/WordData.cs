using System;
using System.Collections.Generic;

[Serializable]
public class WordData
{
    public string clue;
    public string answer;

    public WordData(string clue, string answer)
    {
        this.clue = clue;
        this.answer = answer.ToUpper();
    }
}

[Serializable]
public class WordList
{
    public List<WordData> words;
}