using System;
using System.Collections.Generic;

[Serializable]
public class KnowledgeItem
{
    public string id;
    public string category;
    public string title;
    public string content;
    public string iconText;
}

[Serializable]
public class KnowledgeDataList
{
    public List<KnowledgeItem> knowledge;
}

[Serializable]
public class QuizQuestion
{
    public string id;
    public string category;
    public string question;
    public string[] options;
    public int correctIndex;
    public string explanation;
}

[Serializable]
public class QuizDataList
{
    public List<QuizQuestion> quizzes;
}
