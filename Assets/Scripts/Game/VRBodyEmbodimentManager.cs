using UnityEngine;
using UnityEngine.InputSystem;

public class VRBodyEmbodimentManager : MonoBehaviour
{
    private ModifiersManager.Embodiment embodiment = ModifiersManager.Embodiment.RightHand;
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
            if (mesh != null) mesh.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = false;
        }

        switch (embodiment)
        {
            case ModifiersManager.Embodiment.Full:
                if (full != null) full.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.Arms:
                if (leftArm != null) leftArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                if (rightArm != null) rightArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.RightArm:
                if (rightArm != null) rightArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.LeftArm:
                if (leftArm != null) leftArm.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.Hands:
                if (leftHand != null) leftHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                if (rightHand != null) rightHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.RightHand:
                if (rightHand != null) rightHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
            case ModifiersManager.Embodiment.LeftHand:
                if (leftHand != null) leftHand.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
                break;
        }
    }
}
