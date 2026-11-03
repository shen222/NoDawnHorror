using UnityEngine;

public sealed class InspectionViewModel : MonoBehaviour
{
    public Transform leftHand, rightHand;
    public Renderer[] healthBoxes;
    public Material liveBoxMaterial, emptyBoxMaterial;
    public int LitBoxCount { get; private set; } = -1;
    private Vector3 leftPosition, rightPosition;
    private Quaternion leftRotation, rightRotation;
    private void Awake()
    {
        leftPosition=leftHand.localPosition; rightPosition=rightHand.localPosition;
        leftRotation=leftHand.localRotation; rightRotation=rightHand.localRotation;
    }
    public void Pose(float visibility, float look, float health)
    {
        // Keep the original player hands and gun pose; looking left raises the watch slightly.
        leftHand.localPosition=leftPosition+Vector3.up*(look<-.2f?.045f:0);
        leftHand.localRotation=leftRotation;
        rightHand.localPosition=rightPosition;
        rightHand.localRotation=rightRotation;
        transform.localPosition=Vector3.down*(1-visibility)*.8f;
        int count=health<=0?0:Mathf.Clamp(Mathf.CeilToInt(health*5),1,5);
        if(count==LitBoxCount) return;
        LitBoxCount=count;
        if(healthBoxes==null) return;
        for(int i=0;i<healthBoxes.Length;i++)
            if(healthBoxes[i]!=null) healthBoxes[i].sharedMaterial=i<count?liveBoxMaterial:emptyBoxMaterial;
    }
}