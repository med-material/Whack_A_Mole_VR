using UnityEngine;
using UnityEngine.InputSystem;

public class VRBodyEmbodimentManager : MonoBehaviour
{
    private ModifiersManager.Embodiment embodiment = ModifiersManager.Embodiment.RightHand;

    [SerializeField]
    private GameObject sittingBench;

    public void SetEmbodiment(ModifiersManager.Embodiment selectedEmbodiment)
    {
        this.embodiment = selectedEmbodiment;

        Transform full = this.gameObject.transform.Find("H_Full");
        Transform leftArm = this.gameObject.transform.Find("H_LeftArm");
        Transform rightArm = this.gameObject.transform.Find("H_RightArm");
        Transform leftHand = this.gameObject.transform.Find("H_LeftHand");
        Transform rightHand = this.gameObject.transform.Find("H_RightHand");

        foreach (Transform mesh in new Transform[] { full, leftArm, rightArm, leftHand, rightHand })
        {
            mesh.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = false;
        }

        sittingBench.GetComponent<MeshRenderer>().enabled = false;

        switch (embodiment)
        {
            case ModifiersManager.Embodiment.Full:
                full.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                sittingBench.GetComponent<MeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.Arms:
                leftArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                rightArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.RightArm:
                rightArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.LeftArm:
                leftArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.Hands:
                leftHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                rightHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.RightHand:
                rightHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.LeftHand:
                leftHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
        }
    }
}
