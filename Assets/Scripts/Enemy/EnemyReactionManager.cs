using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//A script that manages what appears when an enemy enters a state
public class EnemyReactionManager : MonoBehaviour
{
    [SerializeField] EnemyController enemyController;
    [SerializeField] TMP_Text reactionText;
    [SerializeField] string[] alertedStrings;
    [SerializeField] string[] surprisedStrings;
    [SerializeField] string[] finishInvestigateStrings;
    [SerializeField] Animator reactAnimator;

    // Start is called before the first frame update
    void OnEnable()
    {
        int randIndex = 0;
        switch (enemyController.currentState)
        {
            case EnemyController.EnemyState.Surprised:
                randIndex = Random.Range(0, surprisedStrings.Length);
                reactionText.text = surprisedStrings[randIndex];
                break;
            case EnemyController.EnemyState.Alerted:
                randIndex = Random.Range(0, alertedStrings.Length);
                reactionText.text = surprisedStrings[randIndex];
                break;
            case EnemyController.EnemyState.FinishedInvestigating:
                randIndex = Random.Range(0, finishInvestigateStrings.Length);
                reactionText.text = surprisedStrings[randIndex];
                break;
            case EnemyController.EnemyState.Sleeping:
                break;
            case EnemyController.EnemyState.KnockedOut:
                break;
            default:
                reactionText.text = "UNDEFINED";
                break;
        }

        reactAnimator.SetTrigger("React");
        StartCoroutine(DisableAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(reactAnimator.GetCurrentAnimatorStateInfo(0).length);

        // Disable the script or GameObject
        enemyController.enemyReaction.SetActive(false);
    }

}
