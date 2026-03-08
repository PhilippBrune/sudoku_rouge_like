using System.Collections;
using UnityEngine;

public class DocumentReviewScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ReviewDocuments());
    }

    IEnumerator ReviewDocuments()
    {
        // Replace this with your actual document names and paths
        string[] documentNames = { "Document1", "Document2", "Document3" };

        foreach (string document in documentNames)
        {
            // Replace this with your actual document review logic
            string reviewResult = ReviewDocument(document);

            // Here you can print or do whatever you need with the review result
            Debug.Log("Review of " + document + ": " + reviewResult);

            // Wait for a second before moving on to the next document
            yield return new WaitForSeconds(1f);
        }
    }

    string ReviewDocument(string documentName)
    {
        // This is just a placeholder. Replace it with your actual document review logic.
        return "Under Review";
    }
}