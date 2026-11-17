using UnityEngine;

public class ComparisonPedestalTrigger : MonoBehaviour
{
    public bool isLeftPedestal = true;
    public TextMesh labelTextMesh;

    private void OnMouseDown()
    {
        OnPedestalClicked();
    }

    public void OnPedestalClicked()
    {
        if (OrganComparisonController.Instance == null) return;

        if (isLeftPedestal)
        {
            if (OrganComparisonController.Instance.ExtractedOrgan != null)
            {
                OrganComparisonController.Instance.SnapOrganToPedestalA();
            }
            else if (OrganComparisonController.Instance.OrganA != null)
            {
                // Toggle / Return
                OrganComparisonController.Instance.ExtractOrganToFront(OrganComparisonController.Instance.OrganA);
            }
        }
        else
        {
            if (OrganComparisonController.Instance.ExtractedOrgan != null)
            {
                OrganComparisonController.Instance.SnapOrganToPedestalB();
            }
            else if (OrganComparisonController.Instance.OrganB != null)
            {
                // Toggle / Return
                OrganComparisonController.Instance.ExtractOrganToFront(OrganComparisonController.Instance.OrganB);
            }
        }
    }
}
