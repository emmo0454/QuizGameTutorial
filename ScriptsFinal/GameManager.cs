using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private Question[] _questions = null;
    public Question[] Questions{ get { return _questions; }}

    [SerializeField] GameEvents events = null;

    // [SerializeField] Animator timerAnimator = null;
    [SerializeField] TextMeshProUGUI timerText = null;

    private List<AnswerData> PickedAnswers = new List<AnswerData>();
    private List<int> FinishedQuestions = new List<int>();
    private int currentQuestion = 0;

    private IEnumerator IE_WaitTillNextRound = null;
    private IEnumerator IE_StartTimer = null;

    private bool IsFinished{
      get{
        return(FinishedQuestions.Count < Questions.Length) ? false : true;
      }
    }

    void OnEnable(){
      events.UpdateQuestionAnswer += UpdateAnswers;
    }
    void OnDisable(){
      events.UpdateQuestionAnswer -= UpdateAnswers;

    }
    void Awake(){
        events.CurrentFinalScore = 0;
    }

    void Start()
    {
      LoadQuestions();


      var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
      UnityEngine.Random.InitState(seed);

      Display();
    }

    public void UpdateAnswers(AnswerData newAnswer){
      if(Questions[currentQuestion].GetAnswerType == Question.AnswerType.Single){
        foreach(var answer in PickedAnswers){
          if(answer != newAnswer){
            answer.Reset();
          }
          PickedAnswers.Clear();
          PickedAnswers.Add(newAnswer);
        }
      }else{
        bool alreadyPicked = PickedAnswers.Exists(x => x == newAnswer);
        if(alreadyPicked){
          PickedAnswers.Remove(newAnswer);
        }else{
          PickedAnswers.Add(newAnswer);
        }
      }
    }

    public void EraseAnswers()
    {
      PickedAnswers = new List<AnswerData>();
    }

    void Display()
    {
        EraseAnswers();
        var question = GetRandomQuestion();

        if(events.UpdateQuestionUI != null){
          events.UpdateQuestionUI(question);
        }else{Debug.LogWarning("Something went wrong, GameEvents.UpdateQuestionUI is null.");}

        if(question.UseTimer){
          UpdateTimer(question.UseTimer);
        }
    }

    public void Accept()
    {
      UpdateTimer(false);
      bool isCorrect = CheckAnswers();
      FinishedQuestions.Add(currentQuestion);

      UpdateScore((isCorrect) ? Questions[currentQuestion].AddScore : -Questions[currentQuestion].AddScore);

      var type = (IsFinished) ? UIManager.ResolutionScreenType.Finish : (isCorrect) ? UIManager.ResolutionScreenType.Correct : UIManager.ResolutionScreenType.Incorrect;

      if (events.DisplayResolutionScreen != null){
        events.DisplayResolutionScreen(type, Questions[currentQuestion].AddScore);
      }
      if(type != UIManager.ResolutionScreenType.Finish){
          if(IE_WaitTillNextRound != null){
          StopCoroutine(IE_WaitTillNextRound);
          }
          IE_WaitTillNextRound = WaitTillNextRound();
          StartCoroutine(IE_WaitTillNextRound);
        }
    }

    void UpdateTimer (bool state){
      switch(state){
        case true:
          IE_StartTimer = StartTimer();
          StartCoroutine(IE_StartTimer);
          break;
        case false:
          if(IE_StartTimer != null){
            StopCoroutine(IE_StartTimer);
          }
          break;
      }
    }

    IEnumerator StartTimer(){
      var totalTime = Questions[currentQuestion].Timer;
      var timeLeft = totalTime;

      while(timeLeft > 0){
        timeLeft --;
        timerText.text = timeLeft.ToString();
        yield return new WaitForSeconds(1.0f);
      }
      Accept();
    }

    IEnumerator WaitTillNextRound(){
      yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
      Display();
    }





    Question GetRandomQuestion ()
    {
      var randomIndex = GetRandomQuestionIndex();
      currentQuestion = randomIndex;

      return Questions[currentQuestion];

    }

    int GetRandomQuestionIndex ()
    {
      var random = 0;
      if(FinishedQuestions.Count < Questions.Length){
        do{
          random = UnityEngine.Random.Range(0, Questions.Length);

        }while(FinishedQuestions.Contains(random) || random == currentQuestion);
      }
      return random;
    }

    bool CheckAnswers(){
      if(CompareAnswers()){
        return false;
      }return true;
    }

    bool CompareAnswers(){
      if(PickedAnswers.Count > 0){
        List<int> _correct = Questions[currentQuestion].GetCorrectAnswers();
        List<int> _picked = PickedAnswers.Select(x=> x.AnswerIndex).ToList();

        var f = _correct.Except(_picked).ToList();
        var s = _picked.Except(_correct).ToList();

        return !f.Any() && !s.Any();
      }return false;
    }

    void LoadQuestions()
    {
      Object[] objs = Resources.LoadAll("Questions", typeof(Question));
      _questions = new Question[objs.Length];
      for (int i = 0; i < objs.Length; i++){
        _questions[i] = (Question)objs[i];
      }
    }

    private void UpdateScore (int add){
      events.CurrentFinalScore += add;

      if(events.ScoreUpdated != null){
        events.ScoreUpdated();
      }
    }
}
