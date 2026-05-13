using UnityEngine;

namespace HappyHarvest
{
    [CreateAssetMenu(fileName = "New Question", menuName = "HappyHarvest/Question")]
    public class QuestionData : ScriptableObject
    {
        [TextArea(3, 5)]
        public string questionText; 

        // Şık sayısını 4 yaptık
        public string[] answers = new string[4]; 

        // Doğru cevap artık 0 ile 3 arasında (0: A, 1: B, 2: C, 3: D)
        [Range(0, 3)]
        public int correctAnswerIndex; 
    }
}