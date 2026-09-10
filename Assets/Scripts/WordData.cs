using System;
using System.Collections.Generic;

[Serializable]
public class WordData
{
    public string clue;
    public string answer;
    public string hint;

    public WordData(string clue, string answer, string hint)
    {
        this.clue = clue;
        this.answer = answer.ToUpper();
        this.hint = hint;
    }
}

[Serializable]
public class WordList
{
    public List<WordData> words;
}